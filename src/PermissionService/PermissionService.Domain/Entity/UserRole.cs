using System.ComponentModel;
using CommunalService.Domain.Entity;
using FreeSql.DataAnnotations;

namespace PermissionService.Domain.Entity;

/// <summary>
/// 账号角色绑定同时携带租户范围；平台角色以 PlatformId 隔离，商户角色以 MerchantId 隔离。
/// </summary>
[Table(Name = "user_role")]
[Index("uk_user_primary_role", "UserId", true)]
public sealed class UserRole : BaseEntity
{
    [Description("用户ID")]
    /// <summary>用户ID</summary>
    public long UserId { get; set; }

    [Description("角色ID")]
    /// <summary>角色ID</summary>
    public long RoleId { get; set; }

    [Description("平台ID，平台角色必填")]
    /// <summary>平台ID，平台角色必填</summary>
    public long PlatformId { get; set; }

    [Description("商户ID，商户角色必填")]
    /// <summary>商户ID，商户角色必填</summary>
    public long MerchantId { get; set; }
}
