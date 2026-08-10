namespace CommunalService.Domain.Infrastructure.Consul;

/// <summary>
/// 定义服务注册与注销的契约（应用层接口）
/// </summary>
public interface IConsulServiceRegistry
{
    /// <summary>
    /// 将当前服务实例注册到 Consul
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task RegisterAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 从 Consul 注销当前服务实例
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeregisterAsync(CancellationToken cancellationToken = default);
}