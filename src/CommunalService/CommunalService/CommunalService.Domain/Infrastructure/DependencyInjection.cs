//using CommunalService.Domain.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace CommunalService.Domain.Infrastructure;

public static class BaseDependencyInjection
{
    /// <summary>
    /// 服务注册
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    public static void AddBaseInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        Func<IServiceProvider, IFreeSql> fsqlFactory = r =>
        {
            IFreeSql fsql = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.PostgreSQL, configuration["BaseData:connectionString"])
                .UseAdoConnectionPool(true)
                .UseMonitorCommand(cmd => Console.WriteLine($"Sql：{cmd.CommandText}"))
                //.UseAutoSyncStructure(true) //自动同步实体结构到数据库，只有CRUD时才会生成表
                .Build();
            return fsql;
        };
        services.AddSingleton<IFreeSql>(fsqlFactory);

    }
}