using CommunalService.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using UserService.Domain.Entity;
using UserService.Domain.IRepository;
using UserService.Infrastructure.Repository;

namespace UserService.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructure(this WebApplicationBuilder builder)
    {
        builder.AddBasicServices();

        // 仓储在 Infrastructure 实现，Application 层只依赖 Domain 接口。
        builder.Services.AddTransient<IUserRepository, UserRepository>();
        builder.Services.AddTransient<IAddressRepository, AddressRepository>();
        builder.Services.AddTransient<IFavoriteRepository, FavoriteRepository>();
    }
}
