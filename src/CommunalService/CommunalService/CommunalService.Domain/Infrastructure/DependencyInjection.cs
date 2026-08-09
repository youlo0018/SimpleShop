//using CommunalService.Domain.Infrastructure;

using AgileConfig.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


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
        ConfigClientOptions  agileConfigOptions = new ConfigClientOptions()
        {
            AppId = builder.Configuration["AgileConfig:appId"],
            Tag = builder.Configuration["AgileConfig:tag"],
            Secret = builder.Configuration["AgileConfig:secret"],
            Nodes = builder.Configuration["AgileConfig:nodes"],
            Name = builder.Configuration["AgileConfig:name"],
            ENV = builder.Configuration[ "AgileConfig:env"]
        };
        builder.Host.UseAgileConfig(agileConfigOptions);
        Func<IServiceProvider, IFreeSql> fsqlFactory = r =>
        {
            IFreeSql fsql = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.PostgreSQL,builder.Configuration["BaseData:connectionString"])
                .UseAdoConnectionPool(true)
                .UseMonitorCommand(cmd => Console.WriteLine($"Sql：{cmd.CommandText}"))
                //.UseAutoSyncStructure(true) //自动同步实体结构到数据库，只有CRUD时才会生成表
                .Build();
            return fsql;
        };
        builder.Services.AddSingleton<IFreeSql>(fsqlFactory);

    }
}