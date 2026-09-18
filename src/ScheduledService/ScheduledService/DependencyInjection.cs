using AgileConfig.Client;
using CommunalService.Domain.Infrastructure;
using CommunalService.Domain;
using CommunalService.Domain.Infrastructure.Locks;
using CommunalService.Domain.Infrastructure.Redis;
using CommunalService.Domain.Infrastructure.Snowflake;
using CommunalService.Domain.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FreeSql;
using Microsoft.AspNetCore.Hosting;
using OrderService.Infrastructure.Repository;
using OrderService.Infrastructure.ExternalServices;
using OrderService.Infrastructure;
using RabbitMQ.Client;
using ScheduledService.Compensation;
using ScheduledService.Jobs;
using StackExchange.Redis;
using YukeTools;

namespace ScheduledService;

public static class DependencyInjection
{
    /// <summary>依赖注入/启动扩展：统一注册入口（漏注册会在启动时暴露）。</summary>
    public static IServiceCollection AddScheduledJobs(
        this IServiceCollection services,
        IConfigurationRoot configuration)
    {
        // 定时进程也必须读取自己独立的 AgileConfig 应用，避免本地配置漂移。
        ((IConfigurationBuilder)configuration).AddAgileConfig(options =>
        {
            options.AppId = configuration["AgileConfig:appId"];
            options.Secret = configuration["AgileConfig:secret"];
            options.Nodes = configuration["AgileConfig:nodes"];
            options.Name = configuration["AgileConfig:name"];
            options.Tag = configuration["AgileConfig:tag"];
            options.ENV = configuration["AgileConfig:env"];
        });

        // 定时进程和 API 进程必须使用同一个 Redis；否则分布式锁只会在各自集群内生效。
        var redis = ConnectionMultiplexer.Connect(configuration["Basic:redisConnectionString"] ?? "localhost");
        services.AddSingleton<IConnectionMultiplexer>(redis);
        services.AddSingleton<IDatabase>(redis.GetDatabase(configuration["Basic:redisDb"].ToInt()));
        services.AddSingleton<IDistributedLock, RedisDistributedLock>();
        services.AddSingleton<RedisWorkerIdProvider>();
        services.AddHostedService<WorkerIdBackgroundService>();

        // 订单仓储默认 IFreeSql 指向订单库；关单、发货和订单查询都依赖该连接。
        services.AddSingleton<IFreeSql>(_ => new FreeSql.FreeSqlBuilder()
            .UseConnectionString(FreeSql.DataType.PostgreSQL,
                configuration["Order:sqlConnectionString"] ?? throw new InvalidOperationException("缺少 Order:sqlConnectionString"))
            .UseAdoConnectionPool(true)
            .Build());

        // 补偿表属于定时项目自有库；用 keyed 实例隔离，避免与订单库连接混淆。
        services.AddKeyedSingleton<IFreeSql>("scheduled", (_, _) => new FreeSql.FreeSqlBuilder()
            .UseConnectionString(FreeSql.DataType.PostgreSQL,
                configuration["Basic:sqlConnectionString"] ?? throw new InvalidOperationException("缺少 Basic:sqlConnectionString"))
            .UseAdoConnectionPool(true)
            .Build());

        // 定时任务复用订单基础设施中的库存客户端，并通过 Consul 发现库存实例。
        services.AddInfrastructure();
        services.AddConsulIntegration(configuration);
        services.AddMemoryCache();
        services.AddSingleton<IStockReleaseCompensationRepository, StockReleaseCompensationRepository>();
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.AddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();

        services.Configure<PaymentTimeoutJobOptions>(configuration.GetSection(PaymentTimeoutJobOptions.SectionName));
        services.AddHostedService<PaymentTimeoutCloseJob>();
        return services;
    }
}
