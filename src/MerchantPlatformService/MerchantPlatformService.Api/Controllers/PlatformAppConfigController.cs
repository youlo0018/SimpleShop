using System.Text.Json;
using CommunalService.Domain;
using CommunalService.Domain.Enums;
using FreeSql;
using Microsoft.AspNetCore.Mvc;
using MerchantPlatformService.Domain.Entity;

using Base = CommunalService.Domain.BaseController;

namespace MerchantPlatformService.Api.Controllers;

/// <summary>
/// 小程序装修配置：配置按 PlatformId 强隔离；游客只能通过平台编码读取已发布配置。
/// </summary>
// 网关与前端统一使用 PlatformConfig 资源名；控制器类名保留业务语义 PlatformAppConfig。
[Route("api/PlatformConfig/[action]")]
public class PlatformAppConfigController(IFreeSql freeSql, TenantContext tenant) : Base
{
    [HttpGet]
    public async Task<ApiResponse> MiniAppPlatforms()
    {
        var items = await freeSql.Select<Platform>()
            .Where(platform => platform.IsEnabled && !platform.IsDeleted)
            .OrderBy(platform => platform.CreatedAt)
            .ToListAsync(platform => new { platform.Id, platform.PlatformCode, platform.PlatformName });
        return Ok(items);
    }

    [HttpGet]
    public async Task<ApiResponse> MiniApp([FromQuery] string platformCode)
    {
        if (string.IsNullOrWhiteSpace(platformCode))
            return Error(BaseApiResponseCode.BadRequest, "缺少平台编码");

        var code = platformCode.Trim();
        var platform = await freeSql.Select<Platform>()
            .Where(item => item.PlatformCode == code && item.IsEnabled && !item.IsDeleted)
            .FirstAsync();
        if (platform is null) return Error(BaseApiResponseCode.NotFound, "平台不存在或未启用");

        var config = await freeSql.Select<PlatformAppConfig>()
            .Where(item => item.PlatformId == platform.Id && item.IsEnabled && item.IsPublished && !item.IsDeleted)
            .FirstAsync();
        return Ok(new
        {
            platform = new { platform.Id, platform.PlatformCode, platform.PlatformName },
            version = config?.PublishVersion ?? 0,
            updatedAt = config?.UpdatedAt ?? platform.UpdatedAt ?? platform.CreatedAt,
            design = ParseDesign(config?.ConfigJson) ?? DefaultDesign(platform.PlatformCode, platform.PlatformName)
        });
    }

    [HttpGet]
    public async Task<ApiResponse> Admin([FromQuery] long platformId)
    {
        var scopedPlatformId = await ResolvePlatformIdAsync(platformId);
        if (scopedPlatformId <= 0)
            return Error(tenant.HasWildcard ? BaseApiResponseCode.BadRequest : BaseApiResponseCode.Forbidden,
                tenant.HasWildcard ? "请选择平台" : "无权配置该平台");

        var platform = await freeSql.Select<Platform>()
            .Where(item => item.Id == scopedPlatformId && !item.IsDeleted).FirstAsync();
        if (platform is null) return Error(BaseApiResponseCode.NotFound, "平台不存在");

        var config = await freeSql.Select<PlatformAppConfig>()
            .Where(item => item.PlatformId == platform.Id && !item.IsDeleted).FirstAsync();
        return Ok(new
        {
            platform = new { platform.Id, platform.PlatformCode, platform.PlatformName },
            version = config?.PublishVersion ?? 0,
            isPublished = config?.IsPublished ?? false,
            design = ParseDesign(config?.ConfigJson) ?? DefaultDesign(platform.PlatformCode, platform.PlatformName)
        });
    }

    [HttpPost]
    public async Task<ApiResponse> Save([FromBody] SavePlatformAppConfigRequest request)
    {
        JsonDocument parsed;
        try
        {
            parsed = JsonDocument.Parse(request.ConfigJson);
        }
        catch (JsonException)
        {
            return Error(BaseApiResponseCode.BadRequest, "页面配置必须是合法JSON");
        }
        using (parsed)
        {
            var normalized = parsed.RootElement.ToString();
            var scopedPlatformId = await ResolvePlatformIdAsync(request.PlatformId);
            if (scopedPlatformId <= 0)
                return Error(tenant.HasWildcard ? BaseApiResponseCode.BadRequest : BaseApiResponseCode.Forbidden,
                    tenant.HasWildcard ? "请选择平台" : "无权配置该平台");

            var platform = await freeSql.Select<Platform>()
                .Where(item => item.Id == scopedPlatformId && !item.IsDeleted).FirstAsync();
            if (platform is null) return Error(BaseApiResponseCode.NotFound, "平台不存在");

            var config = await freeSql.Select<PlatformAppConfig>()
                .Where(item => item.PlatformId == platform.Id && !item.IsDeleted).FirstAsync();
            if (config is null)
            {
                config = new PlatformAppConfig
                {
                    PlatformId = platform.Id, PlatformCode = platform.PlatformCode,
                    ConfigJson = normalized, PublishVersion = request.Publish ? 1 : 0,
                    IsPublished = request.Publish
                };
                await freeSql.Insert(config).ExecuteAffrowsAsync();
            }
            else
            {
                config.PlatformCode = platform.PlatformCode;
                config.ConfigJson = normalized;
                config.UpdatedAt = DateTime.Now;
                if (request.Publish)
                {
                    config.IsPublished = true;
                    config.PublishVersion++;
                }
                await freeSql.Update<PlatformAppConfig>().SetSource(config).ExecuteAffrowsAsync();
            }

            return Ok(new { config.PlatformId, config.PublishVersion, config.IsPublished });
        }
    }

    private async Task<long> ResolvePlatformIdAsync(long requestPlatformId)
    {
        // 平台账号只能维护本平台；通配账号和指定平台的平台账号均收敛到唯一合法平台ID。
        if (tenant.IsPlatform && !tenant.HasWildcard) return tenant.PlatformId;
        if (tenant.IsMerchant) return 0;
        return requestPlatformId;
    }

    private object? ParseDesign(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonDocument.Parse(json).RootElement;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static object DefaultDesign(string code, string name)
    {
        var primary = code.Contains("life", StringComparison.OrdinalIgnoreCase) ? "#0f766e"
            : code.Contains("mall", StringComparison.OrdinalIgnoreCase) ? "#7c3aed" : "#ff4d6d";
        return new
        {
            schemaVersion = 1,
            theme = new { primary, background = "#f5f7fb", tabColor = primary },
            home = new
            {
                appName = name,
                slogan = "本平台专属精选商城",
                notice = "新用户专享好价，登录后立即下单。",
                banners = Array.Empty<object>(),
                modules = new object[]
                {
                    new { type = "quickNav", title = "快捷入口" },
                    new { type = "categories", title = "精选分类", limit = 8 },
                    new { type = "products", key = "recommend", title = "为你推荐", layout = "grid", limit = 10 }
                }
            },
            tabs = new { home = "首页", category = "分类", cart = "购物车", profile = "我的" }
        };
    }
}

public sealed record SavePlatformAppConfigRequest(long PlatformId, string ConfigJson, bool Publish);
