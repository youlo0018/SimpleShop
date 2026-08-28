using MessagePack;

namespace CommunalService.Domain.Contracts.Messages;

[MessagePackObject]
public sealed class PermissionCatalogItem
{
    [Key(0)] public string Code { get; set; } = string.Empty;
    [Key(1)] public string InterfacePath { get; set; } = string.Empty;
}
