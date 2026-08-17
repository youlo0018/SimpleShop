//using CommunalService.Domain.Infrastructure;

using System.Reflection;
using AgileConfig.Client;
using CommunalService.Application.Common;
using CommunalService.Domain.Attributes;
using CommunalService.Domain.Entity;
using CommunalService.Domain.Infrastructure.Consul;
using CommunalService.Domain.Infrastructure.Redis;
using CommunalService.Domain.Infrastructure.Snowflake;
using Consul;
using FluentValidation;
using MagicOnion;
using MediatR;
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

        #endregion

        #region redis注册

        IConnectionMultiplexer redis =
            ConnectionMultiplexer.Connect(builder.Configuration["Basic:redisConnectionString"]);
        builder.Services.AddSingleton<IConnectionMultiplexer>(redis);
        builder.Services.AddSingleton<IDatabase>(redis.GetDatabase(builder.Configuration["Basic:redisDb"].ToInt()));

        #endregion

        #region 雪花id生成注册

        builder.Services.AddSingleton<RedisWorkerIdProvider>();
        builder.Services.AddHostedService<WorkerIdBackgroundService>();

        #endregion

        #region consul注册

        builder.Services.AddConsulIntegration(builder.Configuration);
        builder.Services.AddHealthChecks();

        #endregion

        #region gRPC

        MessagePackSerializer.DefaultOptions =
            MessagePackSerializer.DefaultOptions.WithResolver(MessagePack.Resolvers.StandardResolver.Instance);

        builder.Services.AddGrpc();
        builder.Services.AddMagicOnion(); // 添加 MagicOnion 支持
        // ...

        #endregion

        #region 注册网络服务

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(builder.Configuration["Basic:port:httpport"].ToInt(), listenOptions =>
            {
                listenOptions.UseHttps(); 
                listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
            });

            options.ListenAnyIP(builder.Configuration["Basic:port:grpcport"].ToInt(), listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http2; // 强制 HTTP/2
            });
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
        
        // 1. 绑定 Consul 配置
        services.Configure<ConsulOptions>(sen);


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