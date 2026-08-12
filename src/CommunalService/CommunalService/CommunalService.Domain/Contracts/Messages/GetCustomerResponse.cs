using MessagePack;

namespace CommunalService.Domain.Contracts.Messages;

[MessagePackObject]
public class GetCustomerResponse
{
    [Key(0)]
    public string Name { get; set; } = string.Empty;
    [Key(1)]
    public string Address { get; set; } = string.Empty;
    [Key(2)]
    public string Phone { get; set; } = string.Empty;
    [Key(3)]
    public string data { get; set; }
}