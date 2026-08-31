using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using UserService.Application.Services;

namespace UserService.Application;

public static class DependencyInjection
{
    /// <summary>Application 层服务注册（必须在 builder.Build() 之前调用）。</summary>
    public static void AddApplication(this WebApplicationBuilder builder)
    {
        builder.Services.AddTransient<PermissionCenterClient>();
        builder.Services.AddTransient<AdminTokenIssuer>();
    }

    public static void AddApplication(this WebApplication app)
    {
    }
}
