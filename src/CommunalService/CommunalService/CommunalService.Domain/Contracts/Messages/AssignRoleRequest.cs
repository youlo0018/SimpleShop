using MessagePack;

namespace CommunalService.Domain.Contracts.Messages;

[MessagePackObject]
public class AssignRoleRequest
{
    [Key(0)]
    public long UserId { get; set; }

    [Key(1)]
    public string RoleCode { get; set; } = string.Empty;

    [Key(2)]
    public long PlatformId { get; set; }

    [Key(3)]
    public long MerchantId { get; set; }
}
