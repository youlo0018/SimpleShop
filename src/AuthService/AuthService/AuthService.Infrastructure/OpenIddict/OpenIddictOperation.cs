using AuthService.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using YukeTools;

namespace AuthService.Infrastructure.OpenIddict;

public class OpenIddictOperation
{
    public static async Task SeedClientsAsync(AuthDbContext dbContext)
    {
        await dbContext.Database.EnsureCreatedAsync();

        if (!dbContext.Applications.Any())
        {
            // 前台用户客户端
            var frontendClient = new UserApplication
            {
                ClientId = "frontend-app",
                DisplayName = "Frontend Application",
                ClientType = "frontend",
                Permissions = new List<string>
                {
                    OpenIddictConstants.Permissions.Endpoints.Token,       // "ept:token"
                    OpenIddictConstants.Permissions.GrantTypes.Password,   // "gt:password"
                    OpenIddictConstants.Permissions.GrantTypes.RefreshToken, // "gt:refresh_token"
                    "scp:api1",  // 自定义 scope，确保与 AddServer 中注册的一致
                    "scp:api2"
                }.ToJson()
            };

            // 后台管理客户端
            var backendClient = new UserApplication
            {
                ClientId = "admin-app",
                DisplayName = "Admin Application",
                ClientType = "backend",
                RedirectUris = new List<string> { "https://test.com" }.ToJson(),
                Permissions = new List<string>
                {
                   
                    // 授权端点权限
                    OpenIddictConstants.Permissions.Endpoints.Authorization,   // "ept:authorization"
                    // 令牌端点权限（已有）
                    OpenIddictConstants.Permissions.Endpoints.Token,           // "ept:token"
                    // 授权码授权类型
                    OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode, // "gt:authorization_code"
                    // 刷新令牌授权类型（可选）
                    OpenIddictConstants.Permissions.GrantTypes.RefreshToken,   // "gt:refresh_token"
                    // 响应类型
                    OpenIddictConstants.Permissions.ResponseTypes.Code,        // "resp:code"
                    "scp:api1",  // 自定义 scope，确保与 AddServer 中注册的一致
                    "scp:api2"
                }.ToJson()
            };

            await dbContext.Applications.AddRangeAsync(frontendClient, backendClient);
            await dbContext.SaveChangesAsync();
        }
    }
}