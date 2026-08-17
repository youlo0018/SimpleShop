using CommunalService.Domain.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Domain.IRepository;
using OrderService.Infrastructure.Repository;

namespace OrderService.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// 服务注册
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    public static void AddInfrastructure(
        this WebApplicationBuilder builder)
    {
      
        builder.Services.AddTransient<IOrderRepository, OrderRepository>();

    }
}