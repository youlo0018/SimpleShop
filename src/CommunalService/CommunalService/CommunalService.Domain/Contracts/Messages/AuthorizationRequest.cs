using MessagePack;

namespace CommunalService.Domain.Contracts.Messages;

/// <summary>
/// 登录权限解析请求：由 UserService 在登录/注册时发给权限中心。
/// 权限只认显式角色绑定，不再携带历史 Role 值。
/// </summary>
[MessagePackObject]
public sealed class AuthorizationRequest
{
    /// <summary>用户 ID（UserRole 绑定的主键）。</summary>
    [Key(0)] public long UserId { get; set; }

    /// <summary>登录名（审计/日志用）。</summary>
    [Key(1)] public string UserName { get; set; } = string.Empty;
}
