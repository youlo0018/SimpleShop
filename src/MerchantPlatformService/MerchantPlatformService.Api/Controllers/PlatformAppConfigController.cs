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
        if (platformCode.Trim().Length > 64)
            return Error(BaseApiResponseCode.BadRequest, "平台编码过长");

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
        // 配置为空会让 JsonDocument.Parse 抛 ArgumentNullException 变 500；大小上限防止超大 JSON 落库。
        if (string.IsNullOrWhiteSpace(request.ConfigJson))
            return Error(BaseApiResponseCode.BadRequest, "页面配置不能为空");
        if (request.ConfigJson.Length > 100 * 1024)
            return Error(BaseApiResponseCode.BadRequest, "页面配置不能超过100KB");

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
        // 默认主题用 Apple 蓝，避免未装修平台落到高饱和随机色；平台仍可在装修里覆盖。
        var primary = code.Contains("life", StringComparison.OrdinalIgnoreCase) ? "#30b0c7"
            : code.Contains("mall", StringComparison.OrdinalIgnoreCase) ? "#5e5ce6" : "#0071e3";
        return new
        {
            schemaVersion = 1,
            theme = new { primary, background = "#f5f5f7", tabColor = primary },
            home = new
            {
                appName = name,
                slogan = "本平台专属精选商城",
                notice = "新用户专享好价，登录后立即下单。",
                banners = Array.Empty<object>(),
                modules = new object[]
                {
                    // 金刚区默认四项，后台装修可按平台增删/改图标与跳转。
                    new
                    {
                        type = "quickNav", title = "快捷入口",
                        items = new object[]
                        {
                            new { icon = "/static/line/gift.png", title = "领券中心", linkType = "coupon-center", linkValue = "" },
                            new { icon = "/static/line/order.png", title = "我的订单", linkType = "orders", linkValue = "" },
                            new { icon = "/static/line/cart.png", title = "购物车", linkType = "cart", linkValue = "" },
                            new { icon = "/static/line/pin.png", title = "收货地址", linkType = "address", linkValue = "" }
                        }
                    },
                    new { type = "categories", title = "精选分类", limit = 8 },
                    new { type = "products", key = "recommend", title = "为你推荐", layout = "grid", limit = 10 }
                }
            },
            // 我的服务：profile 页功能宫格，后台可按平台配置（icon/title/linkType/linkValue）。
            // icon 支持线性图标路径（/static/line/*.png）或 emoji；参考凯德星会员中心样式。
            profile = new
            {
                benefits = new object[]
                {
                    new { icon = "/static/line/points.png", title = "积分回馈", linkType = "coupons", linkValue = "" },
                    new { icon = "/static/line/benefit.png", title = "专属活动", linkType = "coupon-center", linkValue = "" },
                    new { icon = "/static/line/star.png", title = "我的收藏", linkType = "favorites", linkValue = "" },
                    new { icon = "/static/line/card.png", title = "更多权益", linkType = "service", linkValue = "" }
                },
                services = new object[]
                {
                    new { icon = "/static/line/order.png", title = "我的订单", linkType = "orders", linkValue = "" },
                    new { icon = "/static/line/cart.png", title = "购物车", linkType = "cart", linkValue = "" },
                    new { icon = "/static/line/record.png", title = "消费记录", linkType = "orders", linkValue = "" },
                    new { icon = "/static/line/gift.png", title = "领券中心", linkType = "coupon-center", linkValue = "" },
                    new { icon = "/static/line/service.png", title = "客服帮助", linkType = "service", linkValue = "" },
                    new { icon = "/static/line/heart.png", title = "我的收藏", linkType = "favorites", linkValue = "" },
                    new { icon = "/static/line/card.png", title = "我的券包", linkType = "coupons", linkValue = "" },
                    new { icon = "/static/line/pin.png", title = "收货地址", linkType = "address", linkValue = "" },
                    new { icon = "/static/line/invoice.png", title = "发票信息", linkType = "service", linkValue = "" },
                    new { icon = "/static/line/info.png", title = "关于我们", linkType = "service", linkValue = "" }
                }
            },
            tabs = new { home = "首页", category = "分类", cart = "购物车", profile = "我的" }
        };
    }
}

public sealed record SavePlatformAppConfigRequest(long PlatformId, string ConfigJson, bool Publish);
