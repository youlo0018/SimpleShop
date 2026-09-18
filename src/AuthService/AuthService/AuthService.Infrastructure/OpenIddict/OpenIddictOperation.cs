using AuthService.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using YukeTools;

namespace AuthService.Infrastructure.OpenIddict;

public class OpenIddictOperation
{
    /// <summary>
    /// 幂等种子客户端：admin-app（后台，password flow、公开客户端）与 frontend-app（保留）。
    /// 已存在时更新权限，保证历史库也能获得 password flow 能力。
    /// </summary>
    public static async Task SeedClientsAsync(AuthDbContext dbContext)
    {
        await dbContext.Database.EnsureCreatedAsync();

        var adminPermissions = new List<string>
        {
            OpenIddictConstants.Permissions.Endpoints.Token,
            OpenIddictConstants.Permissions.GrantTypes.Password,
            OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
            "scp:api1",
            "scp:api2"
        }.ToJson();
        var existingAdmin = await dbContext.Applications.FirstOrDefaultAsync(item => item.ClientId == "admin-app");
        if (existingAdmin is not null)
        {
            existingAdmin.Permissions = adminPermissions;
            // 公开客户端（浏览器 SPA 无 client_secret）；历史库需一并纠正，否则密码流要求客户端认证。
            existingAdmin.ClientType = OpenIddictConstants.ClientTypes.Public;
            await dbContext.SaveChangesAsync();
        }

        if (!dbContext.Applications.Any())
        {
            // 前台用户客户端
            var frontendClient = new UserApplication
            {
                ClientId = "frontend-app",
                DisplayName = "Frontend Application",
                ClientType = OpenIddictConstants.ClientTypes.Public,
                Permissions = new List<string>
                {
                    OpenIddictConstants.Permissions.Endpoints.Token,       // "ept:token"
                    OpenIddictConstants.Permissions.GrantTypes.Password,   // "gt:password"
                    OpenIddictConstants.Permissions.GrantTypes.RefreshToken, // "gt:refresh_token"
                    "scp:api1",  // 自定义 scope，确保与 AddServer 中注册的一致
                    "scp:api2"
                }.ToJson()
            };

            // 后台管理客户端（公开客户端：password flow，无 client_secret）
            var backendClient = new UserApplication
            {
                ClientId = "admin-app",
                DisplayName = "Admin Application",
                ClientType = OpenIddictConstants.ClientTypes.Public,
                Permissions = adminPermissions
            };

            await dbContext.Applications.AddRangeAsync(frontendClient, backendClient);
            await dbContext.SaveChangesAsync();
        }
    }
}
