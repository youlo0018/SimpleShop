using MessagePack;

namespace CommunalService.Domain.Contracts.Messages;

/// <summary>
/// 登录后的权限上下文；通配符 * 仅授予平台管理员，租户范围由下游用于强制数据隔离。
/// </summary>
[MessagePackObject]
public sealed class AuthorizationResponse
{
    [Key(0)] public bool Success { get; set; }
    [Key(1)] public string TenantType { get; set; } = string.Empty;
    [Key(2)] public long PlatformId { get; set; }
    [Key(3)] public long MerchantId { get; set; }
    [Key(4)] public List<string> Permissions { get; set; } = [];
    [Key(5)] public List<string> Roles { get; set; } = [];
}
