using CommunalService.Domain.Enums;
using CommunalService.Domain;
using MediatR;
using UserService.Application.Common;
using UserService.Application.Services;
using UserService.Domain.IRepository;

namespace UserService.Application.Features.User.UpdateUser;

/// <summary>
/// 后台改号：查重（排除自己）→ 全量更新（可选重置密码盐）→ 重新绑定权限中心角色。
/// </summary>
public class UpdateUserCommandHandler(
    IUserRepository repository,
    PermissionCenterClient permissionCenter) : IRequestHandler<UpdateUserCommand, ApiResponse>
{
    /// <summary>处理入口：后台改号：查重（排除自己）→ 全量更新（可选重置密码盐）→ 重新绑定权限中心角色。</summary>
    public async Task<ApiResponse> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await repository.GetByIdAsync(request.Id);
        if (user is null || user.IsDeleted)
            return ApiResults.Fail(BaseApiResponseCode.NotFound, "用户不存在");

        if (await repository.ExistsAsync(request.UserName, request.Phone, request.Id))
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "用户名或手机号已存在");

        user.UserName = request.UserName;
        user.Email = request.Email;
        user.Phone = request.Phone;
        user.Avatar = request.Avatar ?? string.Empty;
        user.Gender = request.Gender;
        user.Birth = request.Birth;
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            var salt = PasswordHasher.NewSalt();
            user.Salt = salt;
            user.pwd = PasswordHasher.Hash(request.Password, salt);
        }

        await repository.UpdateAsync(user);
        await permissionCenter.AssignRoleAsync(user.Id, request.Role, request.PlatformId, request.MerchantId);
        return ApiResults.Ok(new { success = true });
    }
}
