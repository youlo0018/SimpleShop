using CommunalService.Domain.Enums;

namespace CommunalService.Domain.Infrastructure.Consul;

public interface IServiceDiscovery
{
    /// <summary>
    /// 获取指定服务名称的所有健康实例地址（IP:Port 列表）
    /// </summary>
    /// <param name="serviceName">服务名称（如 "user-service"）</param>
    /// <returns>健康实例的地址列表（若无健康实例，返回空列表）</returns>
    Task<IList<ServiceAddressesDto>> GetHealthyServiceAddressesAsync(string serviceName);
    /// <summary>
    /// 通过指定服务名称的轮询获取实例地址（IP:Port 列表）
    /// </summary>
    /// <param name="serviceName"></param>
    /// <returns></returns>
    Task<ServiceAddressesDto> GetPollingAddressAsync(string serviceName);
    
    Task<string> GetPollingAddressAsync(string serviceName, PollingAddressType type=PollingAddressType.Default);
    
}