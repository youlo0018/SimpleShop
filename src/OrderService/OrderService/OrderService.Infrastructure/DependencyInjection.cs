using CommunalService.Domain.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Domain.IRepository;
using OrderService.Infrastructure.Repository;

namespace OrderService.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructure(this WebApplicationBuilder builder)
    {
        builder.Services.AddTransient<IOrderRepository, OrderRepository>();
        builder.Services.AddTransient<IShipmentRepository, ShipmentRepository>();
    }
}


