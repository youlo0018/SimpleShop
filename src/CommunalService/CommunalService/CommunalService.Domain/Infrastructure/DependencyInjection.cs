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
        

    }
}