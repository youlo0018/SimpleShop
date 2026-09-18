namespace CommunalService.Domain.Infrastructure.Consul;

public class ServiceAddressesDto
{
    /// <summary>服务实例 IP。</summary>
    public string IP { get; set; }
    /// <summary>HTTP 端口。</summary>
    public int Port { get; set; }
    /// <summary>gRPC 端口。</summary>
    public int GrpcPort { get; set; }
}
