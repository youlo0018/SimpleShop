using CommunalService.Domain.Contracts.Messages;
using UserService.Domain.Entity;

namespace UserService.Application.Common;

/// <summary>
/// 用户对外输出形状：雪花 Id 由全局 JSON 配置转字符串，权限/租户信息来自权限中心解析结果。
/// </summary>
public static class UserShaper
{
    public static object Shape(User user, AuthorizationResponse? authorization = null)
    {
        return new
        {
            user.Id, user.UserName, user.Email, user.Phone, user.Role, user.Avatar, user.IsEnabled, user.CreatedAt,
            tenantType = authorization?.TenantType ?? "customer",
            platformId = authorization?.PlatformId.ToString() ?? "0",
            merchantId = authorization?.MerchantId.ToString() ?? "0",
            permissions = authorization?.Permissions ?? new List<string>(),
            roles = authorization?.Roles ?? new List<string>()
        };
    }
}
