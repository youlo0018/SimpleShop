using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ProductService.Domain.Entity;
using ProductService.Domain.IRepository;
using ProductService.Infrastructure.Repository;

namespace ProductService.Infrastructure;

public static class DependencyInjection
{
    /// <summary>依赖注入/启动扩展：统一注册入口（漏注册会在启动时暴露）。</summary>
    public static void AddInfrastructure(this WebApplicationBuilder builder)
    {
        builder.Services.AddTransient<IProductAdminRepository, ProductAdminRepository>();
        builder.Services.AddTransient<IProductRepository<Product>, ProductRepository>();
        builder.Services.AddTransient<ICategoryRepository<Category>, CategoryRepository>();
        builder.Services.AddTransient<IBrandRepository<Brand>, BrandRepository>();
        builder.Services.AddTransient<ISkuRepository<Sku>, SkuRepository>();
    }
}
