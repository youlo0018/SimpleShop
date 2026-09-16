using MarketingService.Domain.IRepository;
using MarketingService.Infrastructure.Repository;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace MarketingService.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructure(this WebApplicationBuilder builder)
    {
        builder.Services.AddTransient<IMarketingActivityRepository, MarketingActivityRepository>();
        builder.Services.AddTransient<IMarketingCouponRepository, MarketingCouponRepository>();
    }
}
