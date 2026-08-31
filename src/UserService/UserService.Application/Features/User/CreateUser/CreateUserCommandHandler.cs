using CommunalService.Domain.Enums;
using CommunalService.Domain;
using MediatR;
using UserService.Application.Common;
using UserService.Application.Services;
using UserEntity = UserService.Domain.Entity.User;
using UserService.Domain.IRepository;

namespace UserService.Application.Features.User.CreateUser;

/// <summary>
/// 后台建号：查重 → 落库（Role 只落 customer/admin 两值）→ gRPC 把真实角色（如 merchant-admin）
/// 绑定到权限中心（UserRole 表）。字段级校验见 CreateUserValidator；角色不存在时由权限中心拒绝。
/// </summary>
public class CreateUserCommandHandler(
    IUserRepository repository,
    PermissionCenterClient permissionCenter) : IRequestHandler<CreateUserCommand, ApiResponse>
{
    public async Task<ApiResponse> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var role = string.IsNullOrWhiteSpace(request.Role) ? "customer" : request.Role;
        var user = await CreateUserAsync(request, role);
        if (user is null)
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "用户名或手机号已存在");

        await permissionCenter.AssignRoleAsync(user.Id, request.Role, request.PlatformId, request.MerchantId);
        return ApiResults.Ok(UserShaper.Shape(user));
    }

    private async Task<UserEntity?> CreateUserAsync(CreateUserCommand request, string role)
    {
        if (await repository.ExistsAsync(request.UserName, request.Phone)) return null;

        var salt = PasswordHasher.NewSalt();
        var user = new UserEntity
        {
            UserName = request.UserName.Trim(),
            Email = request.Email ?? string.Empty,
            Phone = request.Phone ?? string.Empty,
            pwd = PasswordHasher.Hash(request.Password, salt),
            Salt = salt,
            Role = role == "admin" ? "admin" : "customer",
            IsAllAgreeAgreement = true,
            IsEnabled = true
        };
        return await repository.InsertAsync(user) ? user : null;
    }
}
