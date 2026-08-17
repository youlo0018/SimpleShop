
using CommunalService.Domain.Infrastructure;
using CustomerService.Domain.Entity;
using CustomerService.Domain.IRepository;
using CustomerService.Infrastructure.Repository;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;


namespace CustomerService.Infrastructure;

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

        
        builder.Services.AddTransient<ICustomerRepository<Customer>, CustomerRepository>();

    }
}