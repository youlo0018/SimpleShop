using Microsoft.AspNetCore.Builder;

namespace InventoryService.Application;

public static class DependencyInjection
{
    /// <summary>依赖注入/启动扩展：统一注册入口（漏注册会在启动时暴露）。</summary>
    public static void AddApplication(this WebApplicationBuilder builder)
    {
    }

    /// <summary>依赖注入/启动扩展：统一注册入口（漏注册会在启动时暴露）。</summary>
    public static void AddApplication(this WebApplication app)
    {
    }
}
