
using CommunalService.Domain.Contracts.Services;
using CommunalService.Domain.Infrastructure;
using CustomerService.Domin.Entity;
using CustomerService.Domin.IRepository;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;


namespace CustomerService.Application;

public static class DependencyInjection
{
  /// <summary>
    /// 服务注册
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    public static void AddApplicationInfrastructure(
        this WebApplicationBuilder builder)
    {
        
       

    }
    public static void AddApplicationInfrastructure(
        this WebApplication app)
    {
      

    }
}