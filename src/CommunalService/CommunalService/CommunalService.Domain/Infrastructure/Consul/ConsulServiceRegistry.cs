using Consul;
using Microsoft.Extensions.Options;

namespace CommunalService.Domain.Infrastructure.Consul;

/// <summary>
/// 使用 Consul 客户端实现服务注册与注销
/// </summary>
public class ConsulServiceRegistry : IConsulServiceRegistry
{
    private readonly IConsulClient _consulClient;
    private readonly ConsulOptions _options;
    private readonly string _serviceId; // 当前实例的唯一 ID

    public ConsulServiceRegistry(IConsulClient consulClient, IOptions<ConsulOptions> options)
    {
        _consulClient = consulClient;
        _options = options.Value;

        // 如果外部未指定 ServiceId，则使用服务名 + GUID 生成唯一 ID
        _serviceId = _options.ServiceId ?? $"{_options.ServiceName}-{Guid.NewGuid():N}";
    }

    /// <summary>
    /// 注册服务到 Consul
    /// </summary>
    public async Task RegisterAsync(CancellationToken cancellationToken = default)
    {
        // 构建健康检查配置
        var healthCheck = new AgentServiceCheck
        {
            // 健康检查的 HTTP 端点地址（Consul 会定期 GET 此地址）
            HTTP = BuildHealthCheckUrl(),
            // 检查间隔
            Interval = TimeSpan.FromSeconds(_options.HealthCheckIntervalSeconds),
            // 超时时间（若超过此时间未响应，视为不健康）
            Timeout = TimeSpan.FromSeconds(5),
            // 若服务持续不健康超过此时间，Consul 自动注销该实例
            DeregisterCriticalServiceAfter = TimeSpan.FromMinutes(1)
        };

        // 构建完整的服务注册信息
        var registration = new AgentServiceRegistration
        {
            ID = _serviceId, // 实例唯一 ID
            Name = _options.ServiceName, // 服务名称（用于发现）
            Address = _options.ServiceAddress, // 服务 IP/主机名
            Port = _options.ServicePort, // 服务端口
            Meta =_options.MetaData,
            Check = healthCheck // 健康检查配置
        };

        // 调用 Consul API 执行注册
        await _consulClient.Agent.ServiceRegister(registration, cancellationToken);
    }

    /// <summary>
    /// 注销服务实例（通常在应用停止时调用）
    /// </summary>
    public async Task DeregisterAsync(CancellationToken cancellationToken = default)
    {
        await _consulClient.Agent.ServiceDeregister(_serviceId, cancellationToken);
    }

    /// <summary>
    /// 构建完整的健康检查 URL（包含协议、主机、端口、路径）
    /// </summary>
    private string BuildHealthCheckUrl()
    {
        // 根据地址是否以 "https" 开头决定协议
        var scheme = _options.ServiceAddress.StartsWith("https", StringComparison.OrdinalIgnoreCase)
            ? "https"
            : "http";
        return $"{scheme}://{_options.ServiceAddress}:{_options.ServicePort}{_options.HealthCheckEndpoint}";
    }
}