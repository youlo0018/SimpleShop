using MessagePack;

namespace CommunalService.Domain.Contracts.Messages;

[MessagePackObject]
public sealed class LoginResponse
{
    [Key(0)] public long Id { get; set; }
    [Key(1)] public string UserName { get; set; } = string.Empty;
    [Key(2)] public string TenantType { get; set; } = string.Empty;
    [Key(3)] public long PlatformId { get; set; }
    [Key(4)] public long MerchantId { get; set; }
    [Key(5)] public List<string> Permissions { get; set; } = [];
    [Key(6)] public List<string> Roles { get; set; } = [];
}
