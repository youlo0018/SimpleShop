using CommunalService.Domain.Enums;
using CommunalService.Domain;
using CommunalService.Domain.Contracts.Messages;
using MediatR;
using UserService.Application.Common;
using UserService.Application.Services;
using UserEntity = UserService.Domain.Entity.User;
using UserService.Domain.IRepository;

namespace UserService.Application.Features.User.Register;

/// <summary>
/// 商城注册：查重（用户名/手机号）→ 生成盐并散列落库 → 解析权限（新用户为 customer）→ 签发令牌自动登录。
/// </summary>
public class RegisterCommandHandler(
    IUserRepository repository,
    PermissionCenterClient permissionCenter,
    AdminTokenIssuer tokenIssuer) : IRequestHandler<RegisterCommand, ApiResponse>
{
    public async Task<ApiResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var role = request.Role == "admin" ? "customer" : request.Role;
        var user = await CreateUserAsync(request, role);
        if (user is null)
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "用户名或手机号已存在");

        var authorization = await permissionCenter.ResolveAsync(user);
        var token = tokenIssuer.CreateToken(user, authorization);
        return ApiResults.Ok(new { token, user = UserShaper.Shape(user, authorization) });
    }

    private async Task<UserEntity?> CreateUserAsync(RegisterCommand request, string role)
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
            Role = role,
            IsAllAgreeAgreement = true,
            IsEnabled = true
        };
        return await repository.InsertAsync(user) ? user : null;
    }
}
