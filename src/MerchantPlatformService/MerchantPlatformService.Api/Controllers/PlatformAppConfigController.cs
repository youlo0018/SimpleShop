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
/// <summary>平台小程序装修配置入口（游客可读、后台可写）：控制器只做协议转换与装修 JSON 解析。</summary>
    public class PlatformAppConfigController(IFreeSql freeSql, TenantContext tenant) : Base
{
    [HttpGet]
    /// <summary>小程序可选平台列表（GET，游客可访问）：只返回启用平台的编码与名称。</summary>
    public async Task<ApiResponse> MiniAppPlatforms()
    {
        var items = await freeSql.Select<Platform>()
            .Where(platform => platform.IsEnabled && !platform.IsDeleted)
            .OrderBy(platform => platform.CreatedAt)
            .ToListAsync(platform => new { platform.Id, platform.PlatformCode, platform.PlatformName });
        return Ok(items);
    }

    [HttpGet]
    /// <summary>小程序启动装修配置（GET，游客可访问）：按平台编码取已发布配置，缺失回退默认装修。</summary>
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

    /// <summary>
    /// 省市区数据（GET，游客可访问）：平台自定义优先，未配置时返回内置默认全国数据。
    /// 小程序地址表单用它渲染三级下拉，结构为 [{name, children:[{name, children:[{name}]}]}]。
    /// </summary>
    [HttpGet]
    public async Task<ApiResponse> Regions([FromQuery] long platformId)
    {
        var platform = await freeSql.Select<Platform>().Where(item => item.Id == platformId && !item.IsDeleted).FirstAsync();
        if (platform is null) return Error(BaseApiResponseCode.NotFound, "平台不存在");
        var config = await freeSql.Select<PlatformAppConfig>()
            .Where(item => item.PlatformId == platform.Id && !item.IsDeleted).FirstAsync();
        var custom = config?.RegionsJson;
        // 反序列化为普通对象树：JsonDocument 会在方法返回后释放，不能直接序列化其 RootElement。
        if (!string.IsNullOrWhiteSpace(custom))
            return Ok(new { regions = JsonSerializer.Deserialize<object>(custom), isCustom = true });
        return Ok(new { regions = JsonSerializer.Deserialize<object>(await LoadDefaultRegionsAsync()), isCustom = false });
    }

    /// <summary>保存平台自定义省市区数据（POST，platform:update）：JSON 三级结构校验 + 大小上限 2MB。</summary>
    [HttpPost]
    public async Task<ApiResponse> SaveRegions([FromBody] SaveRegionsRequest request)
    {
        // 空值表示恢复内置默认（清除平台自定义）。
        var resetToDefault = string.IsNullOrWhiteSpace(request.RegionsJson);
        if (!resetToDefault && request.RegionsJson.Length > 2 * 1024 * 1024)
            return Error(BaseApiResponseCode.BadRequest, "地区数据不能超过2MB");
        if (!resetToDefault)
        {
            try
            {
                using var document = JsonDocument.Parse(request.RegionsJson);
                if (document.RootElement.ValueKind != JsonValueKind.Array)
                    return Error(BaseApiResponseCode.BadRequest, "地区数据必须是数组");
                if (document.RootElement.GetArrayLength() == 0)
                    return Error(BaseApiResponseCode.BadRequest, "地区数据不能为空数组");
                foreach (var province in document.RootElement.EnumerateArray())
                {
                    if (province.ValueKind != JsonValueKind.Object || string.IsNullOrWhiteSpace(province.GetProperty("name").GetString()))
                        return Error(BaseApiResponseCode.BadRequest, "地区数据缺少名称");
                }
            }
            catch (JsonException)
            {
                return Error(BaseApiResponseCode.BadRequest, "地区数据必须是合法JSON");
            }
        }

        var scopedPlatformId = await ResolvePlatformIdAsync(request.PlatformId);
        if (scopedPlatformId <= 0)
            return Error(tenant.HasWildcard ? BaseApiResponseCode.BadRequest : BaseApiResponseCode.Forbidden,
                tenant.HasWildcard ? "请选择平台" : "无权配置该平台");

        var config = await freeSql.Select<PlatformAppConfig>()
            .Where(item => item.PlatformId == scopedPlatformId && !item.IsDeleted).FirstAsync();
        if (config is null)
        {
            var platform = await freeSql.Select<Platform>().Where(item => item.Id == scopedPlatformId && !item.IsDeleted).FirstAsync();
            config = new PlatformAppConfig { PlatformId = scopedPlatformId, PlatformCode = platform?.PlatformCode ?? string.Empty, RegionsJson = resetToDefault ? null : request.RegionsJson };
            await freeSql.Insert(config).ExecuteAffrowsAsync();
        }
        else
        {
            config.RegionsJson = resetToDefault ? null : request.RegionsJson;
            config.UpdatedAt = DateTime.Now;
            await freeSql.Update<PlatformAppConfig>().SetSource(config).ExecuteAffrowsAsync();
        }
        return Ok(new { success = true, isCustom = !resetToDefault });
    }

    /// <summary>内置省市区数据缓存（随程序发布，只读一次）。</summary>
    private static string? _defaultRegions;

    /// <summary>读取内置省市区数据文件（Data/china-regions.json）。</summary>
    private static async Task<string> LoadDefaultRegionsAsync()
    {
        if (_defaultRegions is not null) return _defaultRegions;
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "china-regions.json");
        _defaultRegions = await System.IO.File.ReadAllTextAsync(path);
        return _defaultRegions;
    }

    [HttpGet]
    /// <summary>后台装修配置（GET，platform:read）：按租户裁剪，返回草稿与发布配置。</summary>
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
    /// <summary>保存装修配置（POST，platform:update）：JSON 校验后写库，publish=true 时递增发布版本。</summary>
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

    /// <summary>辅助处理：ResolvePlatformIdAsync。</summary>
    /// <summary>解析目标平台：平台账号强制本平台，超管按请求参数（私有方法，仅本控制器使用）。</summary>
    private async Task<long> ResolvePlatformIdAsync(long requestPlatformId)
    {
        // 平台账号只能维护本平台；通配账号和指定平台的平台账号均收敛到唯一合法平台ID。
        if (tenant.IsPlatform && !tenant.HasWildcard) return tenant.PlatformId;
        if (tenant.IsMerchant) return 0;
        return requestPlatformId;
    }

    /// <summary>辅助处理：ParseDesign。</summary>
    /// <summary>解析装修 JSON；非法 JSON 返回 null（保存前校验会拦截）。</summary>
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

    /// <summary>内置默认装修：平台未配置时保证小程序可正常渲染。</summary>
    private static object DefaultDesign(string code, string name)
    {
        // 默认主题用凯德星式青绿，避免未装修平台落到高饱和随机色；平台仍可在装修里覆盖。
        var primary = code.Contains("life", StringComparison.OrdinalIgnoreCase) ? "#30b0c7"
            : code.Contains("mall", StringComparison.OrdinalIgnoreCase) ? "#5e5ce6" : "#00c1a2";
        return new
        {
            schemaVersion = 1,
            theme = new { primary, background = "#f5f5f5", tabColor = primary },
            home = new
            {
                appName = name,
                slogan = "本平台专属精选商城",
                notice = "新用户专享好价，登录后立即下单。",
                // 默认使用本地设计好的营销 banner（随小程序静态资源发布），未装修平台也有完整头图。
                banners = new object[]
                {
                    new { image = "/static/banners/banner-1.png", title = "夏季洗护日用优惠", linkType = "products", linkValue = "" },
                    new { image = "/static/banners/banner-2.png", title = "会员日狂欢", linkType = "coupon-center", linkValue = "" },
                    new { image = "/static/banners/banner-3.png", title = "新品尝鲜", linkType = "products", linkValue = "" }
                },
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
                    new { icon = "/static/line-color/points.png", title = "积分回馈", linkType = "coupons", linkValue = "" },
                    new { icon = "/static/line-color/benefit.png", title = "专属活动", linkType = "coupon-center", linkValue = "" },
                    new { icon = "/static/line-color/star.png", title = "我的收藏", linkType = "favorites", linkValue = "" },
                    new { icon = "/static/line-color/card.png", title = "更多权益", linkType = "service", linkValue = "" }
                },
                services = new object[]
                {
                    new { icon = "/static/line/order.png", title = "我的订单", linkType = "orders", linkValue = "" },
                    new { icon = "/static/line/cart.png", title = "购物车", linkType = "cart", linkValue = "" },
                    new { icon = "/static/line/record.png", title = "消费记录", linkType = "orders", linkValue = "" },
                    new { icon = "/static/line/gift.png", title = "我的活动", linkType = "coupon-center", linkValue = "" },
                    new { icon = "/static/line/service.png", title = "客服帮助", linkType = "service", linkValue = "" },
                    new { icon = "/static/line/review.png", title = "评价中心", linkType = "service", linkValue = "" },
                    new { icon = "/static/line/points.png", title = "积分指南", linkType = "coupons", linkValue = "" },
                    new { icon = "/static/line/pin.png", title = "收货地址", linkType = "address", linkValue = "" },
                    new { icon = "/static/line/invoice.png", title = "发票信息", linkType = "service", linkValue = "" },
                    new { icon = "/static/line/mall.png", title = "关于我们", linkType = "service", linkValue = "" }
                }
            },
            tabs = new { home = "首页", category = "商城", cart = "购物车", profile = "我的" }
        };
    }
}

/// <summary>保存平台小程序装修配置（发布开关 + JSON 校验）。</summary>
/// <summary>保存平台省市区数据请求：平台 ID + 三级地区 JSON。</summary>
/// <param name="PlatformId">平台 ID。</param>
/// <param name="RegionsJson">三级地区 JSON 数组字符串。</param>
public sealed record SaveRegionsRequest(long PlatformId, string RegionsJson);

public sealed record SavePlatformAppConfigRequest(long PlatformId, string ConfigJson, bool Publish);
