using CustomerService.Domain.Entity;

namespace CustomerService.Application.Common;

/// <summary>
/// 客户对外输出形状：字段名与商城前端约定保持一致（id/userName/...），
/// 租户固定为 customer（C 端无后台权限），平台为账号所属平台，权限列表为空。
/// </summary>
public static class CustomerShaper
{
    /// <summary>构造客户展示对象（雪花 Id 由全局 JSON 配置转字符串）。</summary>
    public static object Shape(Customer customer) => new
    {
        customer.Id,
        UserName = customer.CustomerName,
        customer.Email,
        customer.Phone,
        customer.Avatar,
        customer.Gender,
        customer.Birth,
        customer.RegisterSource,
        Role = "customer",
        tenantType = "customer",
        platformId = customer.PlatformId.ToString(),
        merchantId = "0",
        permissions = Array.Empty<string>(),
        roles = Array.Empty<string>()
    };
}
