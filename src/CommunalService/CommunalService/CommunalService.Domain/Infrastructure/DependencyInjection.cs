//using CommunalService.Domain.Infrastructure;

using System.Reflection;
using AgileConfig.Client;
using CommunalService.Domain.Attributes;
using CommunalService.Domain.Entity;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SnowflakeId.AutoRegister.Builder;
using SnowflakeId.AutoRegister.Interfaces;
using StackExchange.Redis;
using Yitter.IdGenerator;
using YukeTools;


namespace CommunalService.Domain.Infrastructure;

public static class BaseDependencyInjection
{
    /// <summary>
    /// 服务注册
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    public static void AddBaseInfrastructure(
        this WebApplicationBuilder builder)
    {
        #region 雪花id注册

        var options = new IdGeneratorOptions
        {
            WorkerId = 1, // 从配置读取，确保每个服务实例唯一
            WorkerIdBitLength = 6,
            SeqBitLength = 10
        };
        YitIdHelper.SetIdGenerator(options);

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
    }

    public static async  Task AddBaseInfrastructure(this WebApplication app)
    {
        
     
    }
}