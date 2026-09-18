using CommunalService.Domain;
using MediatR;

namespace UserService.Application.Features.User.UpdateUser;

/// <summary>
/// 后台改号/资料编辑：账号字段 + 个人资料（头像/性别/生日）。
/// Role/PlatformId/MerchantId 用于重新绑定权限中心角色；Password 留空表示不修改。
/// </summary>
public record UpdateUserCommand(
    long Id, string UserName, string Email, string Phone, string Role,
    long PlatformId = 0, long MerchantId = 0, string? Password = null,
    string? Avatar = null, int Gender = 0, DateTime? Birth = null) : IRequest<ApiResponse>;
