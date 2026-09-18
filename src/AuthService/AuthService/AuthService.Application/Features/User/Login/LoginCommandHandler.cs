using System.Data;
using System.Security.Claims;
using CommunalService.Domain.Contracts.Messages;
using CommunalService.Domain.Contracts.Services;
using CommunalService.Domain.Enums;
using CommunalService.Domain.Infrastructure.Consul;
using Grpc.Net.Client;
using MagicOnion.Client;
using MediatR;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace AuthService.Application.Features.User.Login;

/// <summary>
/// 后台登录：UserService gRPC 校验口令并取回租户/权限上下文 → 组装 claims →
/// 由 OpenIddict 令牌端点签发访问令牌（网关按 issuer 验签并转 X-Claim-* 头）。
/// </summary>
public class LoginCommandHandler(IServiceDiscovery consul)
    : IRequestHandler<LoginCommand, (ClaimsPrincipal, string)>
{
    /// <summary>校验后台账号并返回含租户/权限声明的 principal；客户账号禁止从后台登录。</summary>
    public async Task<(ClaimsPrincipal, string)> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var address = await consul.GetPollingAddressAsync("UserService", PollingAddressType.Grpc)
            ?? throw new InvalidOperationException("UserService 不可用");
        using var channel = GrpcChannel.ForAddress($"http://{address}");
        var client = MagicOnionClient.Create<IUserService>(channel);
        var result = await client.LoginAsync(new LoginRequest
        {
            UserName = request.UserName,
            Password = request.Password
        });

        if (result.Id <= 0)
            throw new UnauthorizedAccessException("用户名或密码错误");
        // 账号域分离：客户账号只允许从商城登录（CustomerService 签发客户令牌）。
        if (string.Equals(result.TenantType, "customer", StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("该账号为商城客户，请从小程序登录");

        var identity = new ClaimsIdentity(
            TokenValidationParameters.DefaultAuthenticationType,
            ClaimTypes.Name,
            ClaimTypes.Role);

        // OpenIddict 强制要求 sub 声明；网关入站映射会把 sub 还原为 NameIdentifier。
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, result.Id.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, result.Id.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Name, result.UserName));
        identity.AddClaim(new Claim("tenant_type", result.TenantType));
        identity.AddClaim(new Claim("platform_id", result.PlatformId.ToString()));
        identity.AddClaim(new Claim("merchant_id", result.MerchantId.ToString()));
        foreach (var permission in result.Permissions) identity.AddClaim(new Claim("permission", permission));
        // role 用短名声明，网关按类型名转发 X-Claim-role（ClaimTypes.Role 长 URI 不会被转发）。
        foreach (var role in result.Roles) identity.AddClaim(new Claim("role", role));
        // 令牌受众：网关按受众 + issuer + 证书验签。
        identity.SetAudiences("SimpleShop");
        // OpenIddict 只把"声明了目标"的声明写入令牌；这里把全部声明写入访问令牌（后台网关需要租户/权限）。
        identity.SetDestinations(_ => [OpenIddictConstants.Destinations.AccessToken]);

        return (new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}
