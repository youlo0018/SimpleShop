using MessagePack;

namespace CommunalService.Domain.Contracts.Messages;

[MessagePackObject]
public sealed class AuthorizationRequest
{
    [Key(0)] public long UserId { get; set; }
    [Key(1)] public string LegacyRole { get; set; } = string.Empty;
    [Key(2)] public string UserName { get; set; } = string.Empty;
}
