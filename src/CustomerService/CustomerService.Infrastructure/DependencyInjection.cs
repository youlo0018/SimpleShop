using CommunalService.Domain.Infrastructure;
using CustomerService.Domain.Entity;
using CustomerService.Domain.IRepository;
using CustomerService.Infrastructure.Repository;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace CustomerService.Infrastructure;

/// <summary>客户服务基础设施注册：客户/地址/收藏仓储实现。</summary>
public static class DependencyInjection
{
    /// <summary>注册仓储实现（必须在 builder.Build() 之前调用，漏注册会启动即崩）。</summary>
    public static void AddInfrastructure(this WebApplicationBuilder builder)
    {
        builder.Services.AddTransient<ICustomerRepository<Customer>, CustomerRepository>();
        builder.Services.AddTransient<ICustomerAccountRepository, CustomerRepository>();
        builder.Services.AddTransient<ICustomerAddressRepository, CustomerAddressRepository>();
        builder.Services.AddTransient<ICustomerFavoriteRepository, CustomerFavoriteRepository>();
    }
}
