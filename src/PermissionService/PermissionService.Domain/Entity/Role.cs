using System.ComponentModel;
using CommunalService.Domain.Entity;
using FreeSql.DataAnnotations;

namespace PermissionService.Domain.Entity;

public enum TenantType
{
    Platform = 1,
    Merchant = 2
}

[Table(Name = "role")]
[Index("uk_role_code", "Code", true)]
public sealed class Role : BaseEntity
{
    [Column(StringLength = 80), Description("角色编码")]
    /// <summary>角色编码</summary>
    public string Code { get; set; } = string.Empty;

    [Column(StringLength = 64), Description("角色名称")]
    /// <summary>角色名称</summary>
    public string Name { get; set; } = string.Empty;

    [Description("租户类型：1平台，2商户")]
    /// <summary>租户类型：1平台，2商户</summary>
    public int TenantType { get; set; } = 1;

    [Column(StringLength = 255), Description("角色说明")]
    /// <summary>角色说明</summary>
    public string Description { get; set; } = string.Empty;

    [Description("系统内置角色禁止删除")]
    /// <summary>系统内置角色禁止删除</summary>
    public bool IsSystem { get; set; }
}
