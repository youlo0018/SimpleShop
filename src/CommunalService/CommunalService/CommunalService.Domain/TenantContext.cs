using Microsoft.AspNetCore.Http;

namespace CommunalService.Domain;

/// <summary>
/// 网关把已验签的租户声明转换成请求头；下游只信任网关注入的头，避免客户端伪造租户范围。
/// </summary>
public sealed class TenantContext(IHttpContextAccessor httpContextAccessor)
{
    /// <summary>平台 ID。</summary>
    public long PlatformId => Read("X-Claim-PlatformId");
    /// <summary>商户 ID。</summary>
    public long MerchantId => Read("X-Claim-MerchantId");
    /// <summary>用户 ID（网关登录态注入）。</summary>
    public long UserId => Read("X-Claim-UserId");

    /// <summary>访问令牌 jti（网关从验签后的令牌声明转发）：Redis 会话校验与续期用。</summary>
    public string Jti => httpContextAccessor.HttpContext?.Request.Headers["X-Claim-Jti"].ToString() ?? string.Empty;
    /// <summary>
    /// 租户类型：网关按声明类型转发的头是 X-Claim-tenant_type（下划线），兼容历史大小写形式。
    /// </summary>
    public string TenantType => httpContextAccessor.HttpContext?.Request.Headers["X-Claim-tenant_type"].ToString()
        ?? httpContextAccessor.HttpContext?.Request.Headers["X-Claim-TenantType"].ToString() ?? string.Empty;
    /// <summary>是否拥有通配权限（平台超管：permission 含 *）。</summary>
    public bool HasWildcard => httpContextAccessor.HttpContext?.Request.Headers["X-Claim-Permission"].ToString().Split(',').Contains("*") == true;
    /// <summary>全局超管（通配且无平台/商户归属）：不受租户裁剪限制。</summary>
    private bool IsGlobalAdminScope => HasWildcard && PlatformId == 0 && MerchantId == 0;
    /// <summary>
    /// 平台后台身份：客户令牌即使带 platform_id（账号所属平台）也不得视为平台角色，否则订单/用户列表会被放大到全平台。
    /// </summary>
    public bool IsPlatform => !IsGlobalAdminScope && TenantType != "customer"
        && (TenantType == "platform" || (TenantType != "merchant" && PlatformId > 0 && MerchantId == 0));
    /// <summary>商户后台身份：客户令牌即使带 merchant_id 也不视为商户。</summary>
    public bool IsMerchant => !IsGlobalAdminScope && TenantType != "customer"
        && (TenantType == "merchant" || (TenantType != "platform" && MerchantId > 0));
    /// <summary>客户身份：无后台权限且带登录用户 ID。</summary>
    public bool IsCustomer => !HasWildcard && !IsPlatform && !IsMerchant && UserId > 0;

    /// <summary>内部处理：Read。</summary>
    private long Read(string key)
    {
        var value = httpContextAccessor.HttpContext?.Request.Headers[key].ToString();
        return long.TryParse(value, out var id) ? id : 0;
    }
}
