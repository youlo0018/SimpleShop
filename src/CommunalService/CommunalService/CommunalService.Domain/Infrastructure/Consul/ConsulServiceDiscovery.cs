using Consul;
using Microsoft.Extensions.Caching.Memory;
using YukeTools;

namespace CommunalService.Domain.Infrastructure.Consul;

/// <summary>
/// 使用 Consul 客户端实现服务发现
/// </summary>
public class ConsulServiceDiscovery(IConsulClient consulClient, IMemoryCache memoryCache) : IServiceDiscovery
{
    private readonly IMemoryCache _memoryCache = memoryCache;
    private static string _addressCacheKey = "consul_address";
    private static int _counter = 0;

    /// <summary>
    /// 查询健康实例，只返回状态为 "passing" 的服务地址
    /// </summary>
    public async Task<IList<ServiceAddressesDto>> GetHealthyServiceAddressesAsync(string serviceName)
    {
        // 1. 调用 Consul 的健康检查 API，获取该服务所有实例的状态信息
        var queryResult = await consulClient.Health.Service(serviceName);
    
        // 2. 从返回结果中筛选出健康的实例
        //    判断标准：实例的所有健康检查都通过（没有 critical 状态的检查）
        var addresses = queryResult.Response
            .Where(entry => entry.Checks.All(check => check.Status.Equals(HealthStatus.Passing) ))
            .Select(entry => new ServiceAddressesDto
                {
                     IP = entry.Service.Address,
                     Port = entry.Service.Port
                })
            .ToList();

        // 3. 返回地址列表（如果没有健康实例，返回空列表）
        return addresses;
    }

    public async Task<ServiceAddressesDto> GetPollingAddressAsync(string serviceName)
    {
        IList<ServiceAddressesDto> addresses = _memoryCache.Get(_addressCacheKey) as IList<ServiceAddressesDto>;
        if (addresses.IsNull() || !addresses.Any())
        {
            addresses= await GetHealthyServiceAddressesAsync(serviceName);
            _memoryCache.Set(_addressCacheKey, addresses, TimeSpan.FromSeconds(10));
        }
        if (!addresses.Any())
        {
            Console.WriteLine($"服务 {serviceName} 不可用");
            return null;
        }
      
        // 2. 简单负载均衡：取第一个（可替换为轮询、随机等策略）
        var address = addresses[_counter++ % addresses.Count]; // 例如 "172.17.0.1:5001"
        return address;
    }
}