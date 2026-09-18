using MarketingService.Application.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace MarketingService.Application;

/// <summary>应用层注册入口：优惠计算引擎与营销落账服务。</summary>
public static class DependencyInjection
{
    /// <summary>注册应用服务：优惠计算引擎与落账服务（无状态，单例）。</summary>
    public static void AddApplication(this WebApplicationBuilder builder)
    {
        // 优惠计算与落账服务无状态且被控制器/gRPC/消费者复用，单例注册避免重复构造。
        builder.Services.AddSingleton<DiscountEngine>();
        builder.Services.AddSingleton<MarketingCommitService>();
        // 平台营销快照缓存：配置/启用活动/范围按平台缓存，写操作显式失效 + 30s TTL 兜底。
        builder.Services.AddSingleton<MarketingSnapshotCache>();
    }

    /// <summary>应用层启动钩子（当前无操作，保留统一启动入口）。</summary>
    public static void AddApplication(this WebApplication app) { }
}
