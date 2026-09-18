using MessagePack;

namespace CommunalService.Domain.Contracts.Messages;

[MessagePackObject]
public class AssignRoleRequest
{
    [Key(0)]
    /// <summary>用户 ID（网关登录态注入）。</summary>
    public long UserId { get; set; }

    [Key(1)]
    /// <summary>角色编码（权限中心 Role.Code；customer/空 表示解除绑定）。</summary>
    public string RoleCode { get; set; } = string.Empty;

    [Key(2)]
    /// <summary>平台 ID。</summary>
    public long PlatformId { get; set; }

    [Key(3)]
    /// <summary>商户 ID。</summary>
    public long MerchantId { get; set; }
}
