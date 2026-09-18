using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using UserService.Application.Services;

namespace UserService.Application;

/// <summary>后台账号应用层注册：权限中心访问封装（登录校验与角色绑定）。</summary>
public static class DependencyInjection
{
    /// <summary>Application 层服务注册（必须在 builder.Build() 之前调用）。</summary>
    public static void AddApplication(this WebApplicationBuilder builder)
    {
        builder.Services.AddTransient<PermissionCenterClient>();
    }

    /// <summary>Application 层启动钩子（当前无操作，保留统一启动入口）。</summary>
    public static void AddApplication(this WebApplication app)
    {
    }
}
