using Microsoft.AspNetCore.Http;

namespace CommunalService.Domain;

/// <summary>
/// 网关把已验签的租户声明转换成请求头；下游只信任网关注入的头，避免客户端伪造租户范围。
/// </summary>
public sealed class TenantContext(IHttpContextAccessor httpContextAccessor)
{
    public long PlatformId => Read("X-Claim-PlatformId");
    public long MerchantId => Read("X-Claim-MerchantId");
    public long UserId => Read("X-Claim-UserId");
    public string TenantType => httpContextAccessor.HttpContext?.Request.Headers["X-Claim-TenantType"].ToString() ?? string.Empty;
    public bool HasWildcard => httpContextAccessor.HttpContext?.Request.Headers["X-Claim-Permission"].ToString().Split(',').Contains("*") == true;
    private bool IsGlobalAdminScope => HasWildcard && PlatformId == 0 && MerchantId == 0;
    public bool IsPlatform => !IsGlobalAdminScope && (TenantType == "platform" || (TenantType != "merchant" && PlatformId > 0 && MerchantId == 0));
    public bool IsMerchant => !IsGlobalAdminScope && (TenantType == "merchant" || (TenantType != "platform" && MerchantId > 0));
    public bool IsCustomer => !HasWildcard && !IsPlatform && !IsMerchant && UserId > 0;

    private long Read(string key)
    {
        var value = httpContextAccessor.HttpContext?.Request.Headers[key].ToString();
        return long.TryParse(value, out var id) ? id : 0;
    }
}
