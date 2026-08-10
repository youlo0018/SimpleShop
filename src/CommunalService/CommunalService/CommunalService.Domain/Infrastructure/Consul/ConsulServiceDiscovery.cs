using Consul;

namespace CommunalService.Domain.Infrastructure.Consul;

/// <summary>
/// 使用 Consul 客户端实现服务发现
/// </summary>
public class ConsulServiceDiscovery : IServiceDiscovery
{
    private readonly IConsulClient _consulClient;

    public ConsulServiceDiscovery(IConsulClient consulClient)
    {
        _consulClient = consulClient;
    }

    /// <summary>
    /// 查询健康实例，只返回状态为 "passing" 的服务地址
    /// </summary>
    public async Task<IList<string>> GetHealthyServiceAddressesAsync(string serviceName)
    {
        // 1. 调用 Consul 的健康检查 API，获取该服务所有实例的状态信息
        var queryResult = await _consulClient.Health.Service(serviceName);
    
        // 2. 从返回结果中筛选出健康的实例
        //    判断标准：实例的所有健康检查都通过（没有 critical 状态的检查）
        var addresses = queryResult.Response
            .Where(entry => entry.Checks.All(check => check.Status.Equals(HealthStatus.Passing) ))
            .Select(entry => $"{entry.Service.Address}:{entry.Service.Port}")
            .ToList();

        // 3. 返回地址列表（如果没有健康实例，返回空列表）
        return addresses;
    }
}