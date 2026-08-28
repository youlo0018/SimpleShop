using System.Collections.Concurrent;
using CommunalService.Domain.Enums;
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

    private static readonly ConcurrentDictionary<string, int> Counters = new();

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
            .Where(entry => entry.Checks.All(check => check.Status.Equals(HealthStatus.Passing)))
            .Select(entry => new ServiceAddressesDto
            {
                IP = entry.Service.Address,
                Port = entry.Service.Port,
                GrpcPort = entry.Service.Meta.TryGetValue("GrpcPort", out string grpcPort) ? int.Parse(grpcPort) : 0
            })
            .ToList();

        // 3. 返回地址列表（如果没有健康实例，返回空列表）
        return addresses;
    }

    public async Task<ServiceAddressesDto> GetPollingAddressAsync(string serviceName)
    {
        var addresses = await GetCachedAddressesAsync(serviceName);

        if (!addresses.Any())
        {
            Console.WriteLine($"服务 {serviceName} 不可用");
            return null;
        }

        var counter = Counters.AddOrUpdate(serviceName, 1, (_, value) => value + 1);
        var address = addresses[counter % addresses.Count];
        return address;
    }

    public async Task<string> GetPollingAddressAsync(string serviceName,
        PollingAddressType type = PollingAddressType.Default)
    {
        var addresses = await GetCachedAddressesAsync(serviceName);

        if (!addresses.Any())
        {
            Console.WriteLine($"服务 {serviceName} 不可用");
            return null;
        }


        var counter = Counters.AddOrUpdate(serviceName, 1, (_, value) => value + 1);
        var address = addresses[counter % addresses.Count];
        switch (type)
        {
            case PollingAddressType.Default:
                return $"{address.IP}:{address.Port}";
            case PollingAddressType.Grpc:
                // 兼容 HTTP1 与 MagicOnion 共端口的部署；未显式注册 gRPC 元数据时回退主端口。
                return $"{address.IP}:{(address.GrpcPort > 0 ? address.GrpcPort : address.Port)}";
            default:
                return $"{address.IP}:{address.Port}";
        }
    }

    private async Task<IList<ServiceAddressesDto>> GetCachedAddressesAsync(string serviceName)
    {
        var cacheKey = $"consul:healthy:{serviceName}";
        if (_memoryCache.Get(cacheKey) is IList<ServiceAddressesDto> cached)
        {
            return cached;
        }

        var addresses = await GetHealthyServiceAddressesAsync(serviceName);
        _memoryCache.Set(cacheKey, addresses, TimeSpan.FromSeconds(10));
        return addresses;
    }
}
