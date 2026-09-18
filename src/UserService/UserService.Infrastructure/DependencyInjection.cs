using CommunalService.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using UserService.Domain.Entity;
using UserService.Domain.IRepository;
using UserService.Infrastructure.Repository;

namespace UserService.Infrastructure;

public static class DependencyInjection
{
    /// <summary>依赖注入/启动扩展：统一注册入口（漏注册会在启动时暴露）。</summary>
    public static void AddInfrastructure(this WebApplicationBuilder builder)
    {
        builder.AddBasicServices();

        // 仓储在 Infrastructure 实现，Application 层只依赖 Domain 接口。
        builder.Services.AddTransient<IUserRepository, UserRepository>();
    }
}
