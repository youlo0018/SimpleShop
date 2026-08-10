namespace CommunalService.Domain.Infrastructure.Consul;

/// <summary>
/// Consul 连接配置选项
/// </summary>
public class ConsulOptions
{
    /// <summary>Consul 服务器地址（例如：http://localhost:8500）</summary>
    public string Address { get; set; } = "http://localhost:8500";

    /// <summary>当前服务在 Consul 中注册的名称（例如：order-service）</summary>
    public string ServiceName { get; set; } = "CommunalService";

    /// <summary>当前服务实例的唯一 ID（通常用 ServiceName + GUID）</summary>
    public string ServiceId { get; set; }

    /// <summary>当前服务监听的 IP 或主机名（容器内使用容器名，宿主机使用 localhost）</summary>
    public string ServiceAddress { get; set; } = "localhost";

    /// <summary>当前服务监听的端口</summary>
    public int ServicePort { get; set; } = 5000;

    /// <summary>健康检查端点路径（例如：/health）</summary>
    public string HealthCheckEndpoint { get; set; } = "/health";

    /// <summary>健康检查间隔（秒）</summary>
    public int HealthCheckIntervalSeconds { get; set; } = 10;
}