using MessagePack;

namespace CommunalService.Domain.Contracts.Messages;

[MessagePackObject]
public class GetCustomerRequest
{
    /// <summary>
    /// 客户ID
    /// </summary>
    [Key(0)]
    public long Id { get; set; }
}