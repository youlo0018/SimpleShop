//using CommunalService.Domain.Infrastructure;

using System.Reflection;
using AgileConfig.Client;
using CommunalService.Application.Common;
using CommunalService.Domain.Attributes;
using CommunalService.Domain.Entity;
using CommunalService.Domain.Infrastructure.Consul;
using CommunalService.Domain.Infrastructure.Redis;
using CommunalService.Domain.Infrastructure.Snowflake;
using CommunalService.Domain.Logging;
using CommunalService.Domain.Messaging;
using Consul;
using FluentValidation;
using MagicOnion;
using MediatR;
using MessagePack.Resolvers;
using MessagePack;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Yitter.IdGenerator;
using YukeTools;


namespace CommunalService.Domain;

public static class BaseDependencyInjection
{
    /// <summary>
    /// 服务注册
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    public static void AddBasicServices(
        this WebApplicationBuilder builder)
    {
        #region 注册本地缓存

        builder.Services.AddMemoryCache();
        // 业务日志要从当前 HTTP 请求里取身份和 TraceId。
        builder.Services.AddHttpContextAccessor();
        // 网关注入的租户声明是下游数据隔离统一入口。
        builder.Services.AddSingleton<TenantContext>();

        #endregion


        #region AgileConfig 配置中心注册

        ConfigClientOptions agileConfigOptions = new ConfigClientOptions()
        {
            AppId = builder.Configuration["AgileConfig:appId"],
            Tag = builder.Configuration["AgileConfig:tag"],
            Secret = builder.Configuration["AgileConfig:secret"],
            Nodes = builder.Configuration["AgileConfig:nodes"],
            Name = builder.Configuration["AgileConfig:name"],
            ENV = builder.Configuration["AgileConfig:env"]
        };
        builder.Host.UseAgileConfig(agileConfigOptions);

        #endregion

        #region 数据库注册

        Func<IServiceProvider, IFreeSql> fsqlFactory = r =>
        {
            IFreeSql fsql = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.PostgreSQL, builder.Configuration["Basic:sqlConnectionString"])
                .UseAdoConnectionPool(true)
                .UseMonitorCommand(cmd => Console.WriteLine($"Sql：{cmd.CommandText}"))
                //.UseAutoSyncStructure(true) //自动同步实体结构到数据库，只有CRUD时才会生成表
                .Build();
            //Id使用雪花id自动插入
            fsql.Aop.AuditValue += (s, e) =>
            {
                // 判断条件：属性类型为 long，并且标记了 [Snowflake] 特性，并且当前值为 0
                if (e.Column.CsType == typeof(long) &&
                    e.Property.GetCustomAttribute<SnowflakeAttribute>() != null &&
                    e.Value?.ToString() == "0")
                {
                    // 调用你的雪花ID生成器（例如 Yitter.IdGenerator）生成新ID
                    e.Value = YitIdHelper.NextId();
                }
            };
            //查询时排除软删除数据
            fsql.GlobalFilter
                .Apply<BaseEntity>("SoftDelete", a => a.IsDeleted == false);
            return fsql;
        };
        builder.Services.AddSingleton<IFreeSql>(fsqlFactory);

        // 雪花 Id 超出 JavaScript Number 安全整数范围；统一按字符串序列化，避免前端精度丢失。
        builder.Services.PostConfigure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
            options.JsonSerializerOptions.NumberHandling =
                System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString |
                System.Text.Json.Serialization.JsonNumberHandling.WriteAsString);

        #endregion

        #region redis注册

        IConnectionMultiplexer redis =
            ConnectionMultiplexer.Connect(builder.Configuration["Basic:redisConnectionString"]);
        builder.Services.AddSingleton<IConnectionMultiplexer>(redis);
        builder.Services.AddSingleton<IDatabase>(redis.GetDatabase(builder.Configuration["Basic:redisDb"].ToInt()));

        #endregion

        #region 雪花id生成注册

        builder.Services.AddSingleton<RedisWorkerIdProvider>();
        builder.Services.AddSingleton<Infrastructure.Locks.IDistributedLock, Infrastructure.Locks.RedisDistributedLock>();
        builder.Services.AddHostedService<WorkerIdBackgroundService>();

        #endregion

        #region consul注册

        builder.Services.AddConsulIntegration(builder.Configuration);
        builder.Services.AddHealthChecks();

        #region 消息队列

        builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
        // 懒加载单例：启动阶段不强制连接 MQ，避免本机没有 RabbitMQ 时服务直接挂掉。
        builder.Services.AddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();

        // 日志发布器依赖消息队列；MQ 不可用时会在发布阶段失败，不影响请求主流程。
        builder.Services.AddSingleton<LoggingEventPublisher>();
        builder.Services.AddSingleton<IOperationLogger, OperationLogger>();

        #endregion

        #endregion

        #region gRPC

        MessagePackSerializer.DefaultOptions =
            MessagePackSerializer.DefaultOptions.WithResolver(
                CompositeResolver.Create(DynamicObjectResolver.Instance, StandardResolver.Instance));

        builder.Services.AddGrpc();
        builder.Services.AddMagicOnion(); // 添加 MagicOnion 支持
        // ...

        #endregion

        #region 注册网络服务

        builder.WebHost.ConfigureKestrel(options =>
        {
            var restPort = builder.Configuration["Basic:port:httpport"].ToInt();
            if (restPort <= 0)
            {
                return;
            }

            var grpcPort = builder.Configuration["Basic:port:grpcport"].ToInt();
            if (restPort != grpcPort)
            {
                options.ListenAnyIP(restPort, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http1;
                });

                options.ListenAnyIP(grpcPort, listenOptions =>
                {
                    // 独立 gRPC 端口必须使用明文 HTTP/2；REST 继续保留独立的 HTTP/1 端口。
                    listenOptions.Protocols = HttpProtocols.Http2;
                });
            }
            else
            {
                // 内网暂无纯 gRPC 消费方时允许 REST 与 gRPC 共端口，保持网关兼容性。
                options.ListenAnyIP(restPort, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                });
            }
        });

        #endregion
    }

    /// <summary>
    /// 注册 MediatR
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="handlerAssemblies"></param>
    public static void AddMediatRWithHandlers(this WebApplicationBuilder builder, params Assembly[] handlerAssemblies)
    {
        // 1. 注册 MediatR（自动扫描并注册所有 Handler）

        builder.Services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(handlerAssemblies);
            cfg.LicenseKey = builder.Configuration["Basic:MediatR:LicenseKey"];
        });

        // 2. 注册 FluentValidation（自动扫描并注册所有 Validator）

        builder.Services.AddValidatorsFromAssemblies(handlerAssemblies);
        // 3. 注册 MediatR 管道行为（用于自动验证）
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    }

    public static async Task AddBaseInfrastructure(this WebApplication app)
    {
        app.MapHealthChecks("/health");
        // 顺序很重要：异常兜底包住后续管道，PV 才能记录到真实耗时和最终状态。
        app.UseMiddleware<Logging.Middleware.ExceptionLoggingMiddleware>();
        app.UseMiddleware<Logging.Middleware.PageViewLoggingMiddleware>();
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsInterface && type.GetInterfaces()
                        .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IService<>)))
                {
                    Console.WriteLine($"MagicOnion service interface: {type.FullName}");
                }
            }
        }

        app.MapMagicOnionService();
    }

    public static Task MigrateDatabaseAsync(this WebApplication app, params Type[] entityTypes)
    {
        var freeSql = app.Services.GetRequiredService<IFreeSql>();
        freeSql.CodeFirst.SyncStructure(entityTypes);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 添加 Consul 服务注册与发现功能
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象（用于读取 Consul 配置节）</param>
    public static IServiceCollection AddConsulIntegration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var sen = configuration.GetSection("Basic:Consul");

        if (!sen.GetValue("Enabled", true))
        {
            services.Configure<ConsulOptions>(sen);
            services.AddSingleton<IConsulClient>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<ConsulOptions>>().Value;
                return new ConsulClient(configure => configure.Address = new Uri(options.Address));
            });
            services.AddMemoryCache();
            services.AddSingleton<IServiceDiscovery, ConsulServiceDiscovery>();
            return services;
        }

        // 1. 绑定 Consul 配置
        services.Configure<ConsulOptions>(sen);

        if (string.IsNullOrWhiteSpace(sen["ServiceAddress"]))
        {
            services.PostConfigure<ConsulOptions>(options =>
            {
                options.ServiceAddress = "172.18.0.1";
            });
        }


        // 2. 注册 Consul 客户端（单例）
        services.AddSingleton<IConsulClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ConsulOptions>>().Value;
            return new ConsulClient(cfg => { cfg.Address = new Uri(options.Address); });
        });

        // 3. 注册我们自己的服务
        services.AddSingleton<IConsulServiceRegistry, ConsulServiceRegistry>();
        services.AddSingleton<IServiceDiscovery, ConsulServiceDiscovery>();

        // 4. 注册 IHostedService，实现自动注册/注销
        services.AddHostedService<ConsulHostedService>();

        return services;
    }
}
