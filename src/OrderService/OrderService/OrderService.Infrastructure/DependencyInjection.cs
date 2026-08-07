using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Domin.IRepository;
using OrderService.Infrastructure.Repository;

namespace OrderService.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// 服务注册
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    public static void AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
     
        Func<IServiceProvider, IFreeSql> fsqlFactory = r =>
        {
            IFreeSql fsql = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.PostgreSQL, @"Host=43.226.36.154;Port=5432;Database=simpleshoporder;Username=postgres;Password=Aa123456..;Ssl Mode=Disable;")
                .UseAdoConnectionPool(true)
                .UseMonitorCommand(cmd => Console.WriteLine($"Sql：{cmd.CommandText}"))
                //.UseAutoSyncStructure(true) //自动同步实体结构到数据库，只有CRUD时才会生成表
                .Build();
            return fsql;
        };
        services.AddSingleton<IFreeSql>(fsqlFactory);
        services.AddTransient<IOrderRepository, OrderRepository>();

    }
}