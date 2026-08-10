using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CommunalService.Domain.Infrastructure.Consul;

/// <summary>
/// 利用 IHostedService 在应用启动时自动注册服务，停止时自动注销
/// </summary>
public class ConsulHostedService : IHostedService
{
    private readonly IConsulServiceRegistry _registry;
    private readonly ILogger<ConsulHostedService> _logger;

    public ConsulHostedService(IConsulServiceRegistry registry, ILogger<ConsulHostedService> logger)
    {
        _registry = registry;
        _logger = logger;
    }

    /// <summary>
    /// 应用启动时执行：注册服务
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("正在向 Consul 注册服务...");
        await _registry.RegisterAsync(cancellationToken);
        _logger.LogInformation("服务注册成功。");
    }

    /// <summary>
    /// 应用正常停止时执行：注销服务
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("正在从 Consul 注销服务...");
        await _registry.DeregisterAsync(cancellationToken);
        _logger.LogInformation("服务已注销。");
    }
}