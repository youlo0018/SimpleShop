using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ProductService.Domain.Entity;
using ProductService.Domain.IRepository;
using ProductService.Infrastructure.Repository;

namespace ProductService.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructure(this WebApplicationBuilder builder)
    {
        builder.Services.AddTransient<IProductRepository<Product>, ProductRepository>();
        builder.Services.AddTransient<ICategoryRepository<Category>, CategoryRepository>();
        builder.Services.AddTransient<IBrandRepository<Brand>, BrandRepository>();
        builder.Services.AddTransient<ISkuRepository<Sku>, SkuRepository>();
    }
}
