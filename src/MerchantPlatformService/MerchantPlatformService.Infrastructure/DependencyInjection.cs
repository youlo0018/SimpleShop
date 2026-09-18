using MerchantPlatformService.Domain.IRepository;
using MerchantPlatformService.Infrastructure.Repository;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace MerchantPlatformService.Infrastructure;

public static class DependencyInjection
{
    /// <summary>依赖注入/启动扩展：统一注册入口（漏注册会在启动时暴露）。</summary>
    public static void AddInfrastructure(this WebApplicationBuilder builder)
    {
        builder.Services.AddTransient<IMerchantRepository, MerchantRepository>();
        builder.Services.AddTransient<IPlatformRepository, PlatformRepository>();
        builder.Services.AddTransient<IPlatformConfigRepository, PlatformConfigRepository>();
    }
}
