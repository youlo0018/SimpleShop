using MerchantPlatformService.Domain.IRepository;
using MerchantPlatformService.Infrastructure.Repository;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace MerchantPlatformService.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructure(this WebApplicationBuilder builder)
    {
        builder.Services.AddTransient<IMerchantRepository, MerchantRepository>();
        builder.Services.AddTransient<IPlatformRepository, PlatformRepository>();
        builder.Services.AddTransient<IPlatformConfigRepository, PlatformConfigRepository>();
    }
}
