using MessagePack;

namespace CommunalService.Domain.Contracts.Messages;

[MessagePackObject]
public sealed class LoginRequest
{
    [Key(0)] public string UserName { get; set; } = string.Empty;
    [Key(1)] public string Password { get; set; } = string.Empty;
}
