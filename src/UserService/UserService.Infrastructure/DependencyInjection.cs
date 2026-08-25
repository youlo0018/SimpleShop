
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using CommunalService.Domain;


namespace UserService.Infrastructure;

public static class DependencyInjection
{
  /// <summary>
    /// 服务注册
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    public static void AddInfrastructure(
        this WebApplicationBuilder builder)
    {

        builder.AddBasicServices();
    }
}
