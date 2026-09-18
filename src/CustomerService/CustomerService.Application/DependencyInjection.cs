using CustomerService.Application.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace CustomerService.Application;

/// <summary>应用层注册入口：客户令牌签发等无状态服务。</summary>
public static class DependencyInjection
{
    /// <summary>注册应用服务：客户 JWT 签发（无状态，单例）。</summary>
    public static void AddApplication(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<CustomerTokenIssuer>();
        // 令牌会话服务：依赖 AddBasicServices 注册的 IDatabase（Redis）单例。
        builder.Services.AddSingleton<CustomerTokenService>();
    }

    /// <summary>应用层启动钩子（当前无操作，保留统一启动入口）。</summary>
    public static void AddApplication(this WebApplication app) { }
}
