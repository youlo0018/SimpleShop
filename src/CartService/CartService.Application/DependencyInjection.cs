using CartService.Domain;
using CartService.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace CartService.Application;

public static class DependencyInjection
{
    /// <summary>依赖注入/启动扩展：统一注册入口（漏注册会在启动时暴露）。</summary>
    public static void AddApplication(this WebApplicationBuilder builder)
    {
        // Redis 继续承担分布式锁/缓存职责；购物车事实数据改为 PostgreSQL + FreeSql。
        builder.Services.AddSingleton<ICartStore, FreeSqlCartStore>();
    }

    /// <summary>依赖注入/启动扩展：统一注册入口（漏注册会在启动时暴露）。</summary>
    public static void AddApplication(this WebApplication app)
    {
    }
}
