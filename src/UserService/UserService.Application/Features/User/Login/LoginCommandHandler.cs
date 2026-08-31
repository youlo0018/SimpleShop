using CommunalService.Domain.Enums;
using CommunalService.Domain;
using MediatR;
using UserService.Application.Common;
using UserService.Application.Services;
using UserService.Domain.Entity;
using UserService.Domain.IRepository;

namespace UserService.Application.Features.User.Login;

/// <summary>
/// 登录：验密码（SHA256+盐）→ gRPC 调权限中心解析租户与权限 → 签发含租户声明的 JWT。
/// 网关验签后把声明转成 X-Claim-* 头，下游据此做租户隔离——因此这里必须保证声明准确。
/// </summary>
public class LoginCommandHandler(
    IUserRepository repository,
    PermissionCenterClient permissionCenter,
    AdminTokenIssuer tokenIssuer) : IRequestHandler<LoginCommand, ApiResponse>
{
    public async Task<ApiResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await repository.GetByUserNameAsync(request.UserName);
        if (user is null || !user.IsEnabled || PasswordHasher.Hash(request.Password, user.Salt) != user.pwd)
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "用户名或密码错误");

        var authorization = await permissionCenter.ResolveAsync(user);
        var token = tokenIssuer.CreateToken(user, authorization);
        return ApiResults.Ok(new { token, user = UserShaper.Shape(user, authorization) });
    }
}
