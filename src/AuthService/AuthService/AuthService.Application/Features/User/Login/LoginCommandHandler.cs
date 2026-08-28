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
using OpenIddict.Server.AspNetCore;

namespace AuthService.Application.Features.User.Login;

public class LoginCommandHandler(IServiceDiscovery consul)
    : IRequestHandler<LoginCommand, (ClaimsPrincipal, string)>
{
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
        {
            throw new UnauthorizedAccessException("用户名或密码错误");
        }

        long id = result.Id;
        string name = result.UserName;
        
        // 2. 创建身份标识 (ClaimsIdentity)
        var identity = new ClaimsIdentity(
            TokenValidationParameters.DefaultAuthenticationType,
            ClaimTypes.Name, 
            ClaimTypes.Role);

        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, id.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Name, name));

        var principal = new ClaimsPrincipal(identity);

        // 3. 返回 SignIn 结果，OpenIddict 会自动生成 Token
        return (principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        
    }
}
