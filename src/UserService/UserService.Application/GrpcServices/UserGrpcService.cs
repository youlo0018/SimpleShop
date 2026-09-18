using CommunalService.Domain.Contracts.Messages;
using CommunalService.Domain.Contracts.Services;
using MagicOnion;
using MagicOnion.Server;
using UserService.Application.Common;
using UserService.Application.Services;
using UserService.Domain.IRepository;

namespace UserService.Application.GrpcServices;

/// <summary>
/// 后台账号 gRPC：AuthService 登录时校验口令并取回租户/权限上下文（令牌由 AuthService 的 OpenIddict 签发）。
/// </summary>
public sealed class UserGrpcService(IUserRepository repository, PermissionCenterClient permissionCenter)
    : ServiceBase<IUserService>, IUserService
{
    /// <summary>校验账号口令并返回 Id/用户名/租户/权限；失败返回空响应（Id=0）。</summary>
    public async UnaryResult<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await repository.GetByUserNameAsync(request.UserName);
        if (user is null || !user.IsEnabled || PasswordHasher.Hash(request.Password, user.Salt) != user.pwd)
            return new LoginResponse();

        var authorization = await permissionCenter.ResolveAsync(user);
        return new LoginResponse
        {
            Id = user.Id,
            UserName = user.UserName,
            TenantType = authorization.TenantType,
            PlatformId = authorization.PlatformId,
            MerchantId = authorization.MerchantId,
            Permissions = authorization.Permissions,
            Roles = authorization.Roles
        };
    }
}
