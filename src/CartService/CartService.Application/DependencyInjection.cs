using CartService.Domain;
using CartService.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace CartService.Application;

public static class DependencyInjection
{
    public static void AddApplication(this WebApplicationBuilder builder)
    {
        // Redis 继续承担分布式锁/缓存职责；购物车事实数据改为 PostgreSQL + FreeSql。
        builder.Services.AddSingleton<ICartStore, FreeSqlCartStore>();
    }

    public static void AddApplication(this WebApplication app)
    {
    }
}
