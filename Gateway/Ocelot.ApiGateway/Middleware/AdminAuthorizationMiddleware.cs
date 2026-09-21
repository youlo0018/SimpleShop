using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using CommunalService.Domain;
using CommunalService.Domain.Contracts.Messages;
using CommunalService.Domain.Contracts.Services;
using CommunalService.Domain.Enums;
using CommunalService.Domain.Infrastructure;
using CommunalService.Domain.Infrastructure.Consul;
using Grpc.Net.Client;
using MagicOnion.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;

namespace SimpleShop.Gateway.Middleware;

/// <summary>
/// 管理端统一 RBAC 入口：公开商城接口放行，后台动作校验权限；租户声明再转发给下游做强制数据隔离。
/// </summary>
public sealed class AdminAuthorizationMiddleware(
    RequestDelegate next,
    IConfiguration configuration,
    IMemoryCache cache,
    IServiceDiscovery serviceDiscovery,
    StackExchange.Redis.IConnectionMultiplexer redis)
{
    private static readonly Dictionary<string, string> ProtectedActions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["/gateway/products/CreateProduct"] = "product:create",
        ["/gateway/products/Update"] = "product:create",
        ["/gateway/products/PublishProduct"] = "product:publish",
        ["/gateway/products/OffShelf"] = "product:publish",
        ["/gateway/products/CreateCategory"] = "category:create",
        ["/gateway/products/UpdateCategory"] = "category:create",
        ["/gateway/products/DeleteCategory"] = "category:create",
        ["/gateway/products/DisableCategory"] = "category:create",
        ["/gateway/orders/Shipment"] = "order:ship",
        ["/gateway/orders/Receive"] = "order:receive",
        ["/gateway/orders/Cancel"] = "order:cancel",
        ["/gateway/payments/Refund"] = "refund:approve",
        ["/gateway/payments/ApproveRefund"] = "refund:approve",
        ["/gateway/payments/RejectRefund"] = "refund:approve",
        ["/gateway/reports"] = "dashboard:view",
        ["/gateway/merchants/Create"] = "merchant:create",
        ["/gateway/merchants/Review"] = "merchant:review",
        ["/gateway/merchants/Update"] = "merchant:update",
        ["/gateway/merchants/SetStatus"] = "merchant:update",
        ["/gateway/platforms/Create"] = "platform:create",
        ["/gateway/platforms/Edit"] = "platform:update",
        ["/gateway/platforms/SetEnabled"] = "platform:update",
        ["/gateway/platform-configs/Admin"] = "platform:update",
        ["/gateway/platform-configs/Save"] = "platform:update",
        ["/gateway/users/Create"] = "user:create",
        ["/gateway/users/Update"] = "user:update-status",
        ["/gateway/users/UpdateStatus"] = "user:update-status",
        ["/gateway/marketing/SaveActivity"] = "marketing:create",
        ["/gateway/marketing/SetActivityEnabled"] = "marketing:create",
        ["/gateway/marketing/SaveCouponTemplate"] = "marketing:create",
        ["/gateway/marketing/SetCouponTemplateEnabled"] = "marketing:create",
        ["/gateway/marketing/SaveCouponActivity"] = "marketing:create",
        ["/gateway/marketing/SetCouponActivityEnabled"] = "marketing:create",
        ["/gateway/marketing/SaveConfig"] = "marketing:create"
    };

    private const string CatalogCacheKey = "permission-interface-catalog";

    public async Task InvokeAsync(HttpContext context)
    {
        // 网关是租户声明的唯一可信来源；先清空外部请求头，防止伪造 X-Claim-* 绕过下游隔离。
        foreach (var forgedHeaderName in context.Request.Headers.Keys
                     .Where(headerName => headerName.StartsWith("X-Claim-", StringComparison.OrdinalIgnoreCase))
                     .ToArray())
        {
            context.Request.Headers.Remove(forgedHeaderName);
        }

        var principal = await ValidateTokenAsync(context.Request.Headers.Authorization.ToString());
        if (principal is not null)
        {
            context.User = principal;
            foreach (var claim in principal.Claims.Where(claim => claim.Type is "permission" or "role" or "tenant_type"))
                context.Request.Headers.Append($"X-Claim-{claim.Type}", claim.Value);
            context.Request.Headers["X-Claim-UserId"] = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0";
            // jti 用于下游刷新/登出时定位 Redis 会话键。
            context.Request.Headers["X-Claim-Jti"] = principal.FindFirstValue("jti") ?? string.Empty;
            context.Request.Headers["X-Claim-PlatformId"] = principal.FindFirstValue("platform_id") ?? "0";
            context.Request.Headers["X-Claim-MerchantId"] = principal.FindFirstValue("merchant_id") ?? "0";
        }

        // 先判断路径是否需要权限：公开接口（含 /health）无 token 也放行；需要权限才要求登录（401）。
        var required = ResolveRequiredPermission(context, await GetPermissionCatalogAsync());
        if (required is null)
        {
            await next(context);
            return;
        }

        if (principal is null)
        {
            await WriteAsync(context, StatusCodes.Status401Unauthorized, BaseApiResponseCode.Unauthorized, "请先登录");
            return;
        }


        var permissions = principal.FindAll("permission").Select(claim => claim.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var isBackground = principal.FindFirstValue("tenant_type") is "platform" or "merchant";
        var isPublicList = HttpMethods.IsGet(context.Request.Method) &&
                           (context.Request.Path.StartsWithSegments("/gateway/products") ||
                            context.Request.Path.StartsWithSegments("/gateway/orders"));
        if (isPublicList && !isBackground)
        {
            await next(context);
            return;
        }

        // 商城的用户余额、订单和退款必须强制以当前登录人自查；网关不把后台权限误加到普通用户。
        if (!isBackground && IsCustomerSelfService(context))
        {
            await next(context);
            return;
        }

        if (!isBackground || (!permissions.Contains("*") && !permissions.Contains(required)))
        {
            await WriteAsync(context, StatusCodes.Status403Forbidden, BaseApiResponseCode.Forbidden, "没有访问该功能的权限");
            return;
        }

        await next(context);
    }
    /// <summary>
    /// 根据请求路径解析出需要的权限码。
    /// </summary>
    /// <param name="context"></param>
    /// <param name="catalog"></param>
    /// <returns></returns>

    private string? ResolveRequiredPermission(HttpContext context, List<PermissionCatalogItem> catalog)
    {
        // 商城公开接口：活动专区、商品到手价与店铺信息允许游客浏览（下单/用券仍要求登录）。
        if (context.Request.Path.Equals("/gateway/marketing/ActiveActivities", StringComparison.OrdinalIgnoreCase) ||
            context.Request.Path.Equals("/gateway/marketing/FinalPrice", StringComparison.OrdinalIgnoreCase) ||
            context.Request.Path.Equals("/gateway/merchants/Shop", StringComparison.OrdinalIgnoreCase))
            return null;

        if (context.Request.Path.StartsWithSegments("/gateway/permissions"))
            return "permission:manage";
        if (context.Request.Path.Equals("/gateway/logs", StringComparison.OrdinalIgnoreCase))
            return "report:read";
        if (context.Request.Path.StartsWithSegments("/gateway/reports"))
            return "dashboard:view";
        if (ProtectedActions.TryGetValue(context.Request.Path.Value?.TrimEnd('/') ?? "", out var action))
            return action;

        if (HttpMethods.IsGet(context.Request.Method))
        {
            if (context.Request.Path.StartsWithSegments("/gateway/payments/Refunds")) return "refund:read";
            if (context.Request.Path.StartsWithSegments("/gateway/payments/Payments")) return "order:read";
            if (context.Request.Path.StartsWithSegments("/gateway/users/Users")) return "user:read";
            if (context.Request.Path.StartsWithSegments("/gateway/merchants")) return "merchant:read";
            if (context.Request.Path.StartsWithSegments("/gateway/platforms")) return "platform:read";
            if (context.Request.Path.StartsWithSegments("/gateway/permissions")) return "permission:manage";
            if (context.Request.Path.StartsWithSegments("/gateway/logs")) return "report:read";
            if (context.Request.Path.StartsWithSegments("/gateway/marketing")) return "marketing:read";
        }

        return null;

     
    }

    private async Task<List<PermissionCatalogItem>> GetPermissionCatalogAsync()
    {
        return (await cache.GetOrCreateAsync(CatalogCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
            try
            {
                var address =
                    await serviceDiscovery.GetPollingAddressAsync("PermissionService", PollingAddressType.Grpc);
                using var channel = GrpcChannel.ForAddress($"http://{address}");
                return await MagicOnionClient.Create<IPermissionService>(channel).ListCatalogAsync();
            }
            catch
            {
                return new List<PermissionCatalogItem>();
            }
        }))!;
    }

    private static bool IsCustomerSelfService(HttpContext context)
    {
        if (HttpMethods.IsGet(context.Request.Method) &&
            (context.Request.Path.StartsWithSegments("/gateway/payments/Payments") ||
             context.Request.Path.StartsWithSegments("/gateway/payments/Refunds")))
            return true;

        // 营销用户自助接口：领券中心/我的券包/领券/结算预览，登录后放行，权限校验在服务内部按登录态兜底。
        if (HttpMethods.IsGet(context.Request.Method) &&
            (context.Request.Path.StartsWithSegments("/gateway/marketing/ClaimableCoupons") ||
             context.Request.Path.StartsWithSegments("/gateway/marketing/MyCoupons")))
            return true;

        return context.Request.Method == HttpMethods.Post &&
               (context.Request.Path.StartsWithSegments("/gateway/payments/Refund") ||
                context.Request.Path.StartsWithSegments("/gateway/orders/Receive") ||
                context.Request.Path.StartsWithSegments("/gateway/orders/Cancel") ||
                context.Request.Path.StartsWithSegments("/gateway/marketing/ClaimCoupon") ||
                context.Request.Path.StartsWithSegments("/gateway/marketing/SettlePreview"));
    }

    /// <summary>
    /// 令牌验签：后台令牌由 AuthService（OpenIddict）签发、客户令牌由 CustomerService 签发，
    /// 两者共用对称密钥与受众；历史 UserService 令牌保留兼容（issuer 白名单）。
    /// </summary>
    private async Task<ClaimsPrincipal?> ValidateTokenAsync(string authorizationHeader)
    {
        if (!authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;
        // 令牌格式不合法或过期，直接返回 null（网关不抛异常，避免泄露内部信息）。
        var handler = new JwtSecurityTokenHandler();
        var token = authorizationHeader["Bearer ".Length..].Trim();
        if (!handler.CanReadToken(token))
            return null;
        // 后台令牌由 OpenIddict（AuthService，RS256 自签证书）签发；客户/历史令牌为 HS256 共享密钥。
        var issuer = handler.ReadJwtToken(token).Issuer;
        var isBackendToken = issuer == "https://simpleshop.local/auth";
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuers = isBackendToken
                ? ["https://simpleshop.local/auth"]
                : ["SimpleShop.CustomerService"],
            ValidateAudience = true,
            ValidAudience = "SimpleShop",
            IssuerSigningKey = isBackendToken
                ? new X509SecurityKey(LocalSigningCertificate.LoadOrCreate(configuration))
                : new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                    configuration["Auth:TokenSecret"] ?? "SimpleShop.Dev.Token.Secret.2026")),
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        try
        {
            var principal = handler.ValidateToken(token, parameters, out _);
            // 客户令牌是 Redis 服务端会话：jti 会话不存在（登出/被踢/过期）即视为未登录；
            // 剩余有效期低于 2 小时自动续期到 12 小时，实现"活跃用户临近过期自动续期"。
            if (!isBackendToken && !await EnsureCustomerSessionAsync(principal)) return null;
            return principal;
        }
        catch (Exception exception) when (exception is SecurityTokenException or ArgumentException)
        {
            return null;
        }
    }

    /// <summary>校验并滑动续期客户令牌会话（Redis）。</summary>
    private async Task<bool> EnsureCustomerSessionAsync(ClaimsPrincipal principal)
    {
        var jti = principal.FindFirstValue("jti");
        if (string.IsNullOrWhiteSpace(jti)) return false;
        var database = redis.GetDatabase();
        var key = $"auth:customer:token:{jti}";
        // 先取会话值判存在（KeyTimeToLiveAsync 在不同版本对"键不存在"的返回不一致，不能作为唯一判据）。
        var session = await database.StringGetAsync(key);
        if (session.IsNullOrEmpty) return false;
        var ttl = await database.KeyTimeToLiveAsync(key);
        if (ttl is not null && ttl.Value > TimeSpan.Zero && ttl.Value < TimeSpan.FromHours(2))
            await database.KeyExpireAsync(key, TimeSpan.FromHours(12));
        return true;
    }

    private static async Task WriteAsync(HttpContext context, int statusCode, BaseApiResponseCode code, string message)
    {
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(
            new ApiResponse { Code = code.Value, Message = message, Data = new { } });
    }
}