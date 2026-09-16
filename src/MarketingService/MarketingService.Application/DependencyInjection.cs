using MarketingService.Application.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace MarketingService.Application;

public static class DependencyInjection
{
    public static void AddApplication(this WebApplicationBuilder builder)
    {
        // 优惠计算与落账服务无状态且被控制器/gRPC/消费者复用，单例注册避免重复构造。
        builder.Services.AddSingleton<DiscountEngine>();
        builder.Services.AddSingleton<MarketingCommitService>();
    }

    public static void AddApplication(this WebApplication app) { }
}
