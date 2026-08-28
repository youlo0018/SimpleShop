using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CommunalService.Domain;
using CommunalService.Domain.Contracts.Messages;
using CommunalService.Domain.Contracts.Services;
using CommunalService.Domain.Enums;
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
    IServiceDiscovery serviceDiscovery)
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
        ["/gateway/users/UpdateStatus"] = "user:update-status"
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
            context.Request.Headers["X-Claim-PlatformId"] = principal.FindFirstValue("platform_id") ?? "0";
            context.Request.Headers["X-Claim-MerchantId"] = principal.FindFirstValue("merchant_id") ?? "0";
        }

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

        var permissions = principal.FindAll("permission").Select(claim => claim.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
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

    private string? ResolveRequiredPermission(HttpContext context, List<PermissionCatalogItem> catalog)
    {
        if (context.Request.Path.StartsWithSegments("/gateway/permissions"))
            return "permission:manage";
        if (context.Request.Path.Equals("/gateway/logs", StringComparison.OrdinalIgnoreCase))
            return "report:read";
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
        }
        return null;

        string? MatchCatalog()
        {
            var path = context.Request.Path.Value?.TrimEnd('/') ?? string.Empty;
            var matched = catalog.FirstOrDefault(item =>
                !string.IsNullOrWhiteSpace(item.InterfacePath) && (
                    item.InterfacePath.EndsWith("/**", StringComparison.OrdinalIgnoreCase) ||
                    item.InterfacePath.EndsWith("/*", StringComparison.OrdinalIgnoreCase)
                        ? path.StartsWith(item.InterfacePath[..item.InterfacePath.LastIndexOf('/')], StringComparison.OrdinalIgnoreCase)
                        : path.Equals(item.InterfacePath.TrimEnd('/'), StringComparison.OrdinalIgnoreCase)));
            return matched?.Code;
        }

        // 超级管理员可在权限中心新增权限并绑定接口；网关使用目录缓存动态识别新增的接口权限。
        return MatchCatalog();
    }

    private async Task<List<PermissionCatalogItem>> GetPermissionCatalogAsync()
    {
        return (await cache.GetOrCreateAsync(CatalogCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
            try
            {
                var address = await serviceDiscovery.GetPollingAddressAsync("PermissionService", PollingAddressType.Grpc);
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

        return context.Request.Method == HttpMethods.Post &&
               (context.Request.Path.StartsWithSegments("/gateway/payments/Refund") ||
                context.Request.Path.StartsWithSegments("/gateway/orders/Receive") ||
                context.Request.Path.StartsWithSegments("/gateway/orders/Cancel"));
    }

    private async Task<ClaimsPrincipal?> ValidateTokenAsync(string authorizationHeader)
    {
        if (!authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;
        var handler = new JwtSecurityTokenHandler();
        if (!handler.CanReadToken(authorizationHeader["Bearer ".Length..].Trim()))
            return null;
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "SimpleShop.UserService",
            ValidateAudience = true,
            ValidAudience = "SimpleShop",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                configuration["Auth:TokenSecret"] ?? "SimpleShop.Dev.Token.Secret.2026")),
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        try
        {
            return handler.ValidateToken(authorizationHeader["Bearer ".Length..].Trim(), parameters, out _);
        }
        catch (SecurityTokenException)
        {
            return null;
        }
    }

    private static async Task WriteAsync(HttpContext context, int statusCode, BaseApiResponseCode code, string message)
    {
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new ApiResponse { Code = code.Value, Message = message, Data = new { } });
    }
}
