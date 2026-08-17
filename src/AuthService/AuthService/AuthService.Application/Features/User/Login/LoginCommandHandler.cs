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
//         var address = await consul.GetPollingAddressAsync("UserService", PollingAddressType.Grpc);
//         var channel = GrpcChannel.ForAddress($"http://{address}"); // 实际通过 Consul 获取地址
//         // 2. 使用 MagicOnionClient 创建客户端代理
//         var client = MagicOnionClient.Create<IUserService>(channel);
//
// // 3. 调用服务方法
//         var result = await client.LoginAsync(new LoginRequest()
//         {
//             UserName=request.UserName,
//             Password = request.Password
//             
//         });
        string name = "admin";
        string pwd = "123";
        long id = 123444;
        
        // 2. 创建身份标识 (ClaimsIdentity)
        var identity = new ClaimsIdentity(
            TokenValidationParameters.DefaultAuthenticationType,
            ClaimTypes.Name, 
            ClaimTypes.Role);

        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, id.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Name, name));
        // 可添加更多 Claims，如用户角色等

        var principal = new ClaimsPrincipal(identity);

        // 3. 返回 SignIn 结果，OpenIddict 会自动生成 Token
        return (principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        
    }
}