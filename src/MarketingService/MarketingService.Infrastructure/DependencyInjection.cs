using MarketingService.Domain.IRepository;
using MarketingService.Infrastructure.Repository;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace MarketingService.Infrastructure;

/// <summary>基础设施注册入口：活动/券仓储实现。</summary>
public static class DependencyInjection
{
    /// <summary>注册仓储实现（必须在 builder.Build() 之前调用，漏注册会启动即崩）。</summary>
    public static void AddInfrastructure(this WebApplicationBuilder builder)
    {
        builder.Services.AddTransient<IMarketingActivityRepository, MarketingActivityRepository>();
        builder.Services.AddTransient<IMarketingCouponRepository, MarketingCouponRepository>();
    }
}
