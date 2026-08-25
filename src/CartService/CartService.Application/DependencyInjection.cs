using CartService.Domain;
using CartService.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace CartService.Application;

public static class DependencyInjection
{
    public static void AddApplication(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<ICartStore, RedisCartStore>();
    }

    public static void AddApplication(this WebApplication app)
    {
    }
}
