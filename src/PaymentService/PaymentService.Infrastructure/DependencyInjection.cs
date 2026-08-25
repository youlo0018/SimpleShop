using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using PaymentService.Domain.IRepository;
using PaymentService.Infrastructure.Repository;

namespace PaymentService.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructure(this WebApplicationBuilder builder)
    {
        builder.Services.AddTransient<IPaymentOrderRepository, PaymentOrderRepository>();
    }
}
