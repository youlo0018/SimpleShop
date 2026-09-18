using CommunalService.Domain.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Infrastructure.ExternalServices;
using OrderService.Domain.IRepository;
using OrderService.Infrastructure.Repository;

namespace OrderService.Infrastructure;

public static class DependencyInjection
{
    /// <summary>依赖注入/启动扩展：统一注册入口（漏注册会在启动时暴露）。</summary>
    public static void AddInfrastructure(this WebApplicationBuilder builder)
    {
        // 下单和超时关单都要调用库存服务，这里统一注册，避免不同进程配置漂移。
        builder.Services.AddInfrastructure();
    }

    /// <summary>依赖注入/启动扩展：统一注册入口（漏注册会在启动时暴露）。</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddTransient<IOrderRepository, OrderRepository>();
        services.AddTransient<IShipmentRepository, ShipmentRepository>();
        services.AddSingleton<InventoryClient>();
        services.AddSingleton<MarketingClient>();
        return services;
    }
}
