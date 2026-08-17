namespace CommunalService.Domain.Infrastructure.Consul;

public class ServiceAddressesDto
{
    public string IP { get; set; }
    public int Port { get; set; }
    public int GrpcPort { get; set; }
}