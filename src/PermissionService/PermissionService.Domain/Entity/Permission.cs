using System.ComponentModel;
using CommunalService.Domain.Entity;
using FreeSql.DataAnnotations;

namespace PermissionService.Domain.Entity;

[Table(Name = "permission")]
[Index("uk_permission_code", "Code", true)]
public sealed class Permission : BaseEntity
{
    [Column(StringLength = 80), Description("权限编码，例如 product:create")]
    public string Code { get; set; } = string.Empty;

    [Column(StringLength = 64), Description("权限名称")]
    public string Name { get; set; } = string.Empty;

    [Column(StringLength = 40), Description("资源")]
    public string Resource { get; set; } = string.Empty;

    [Column(StringLength = 40), Description("动作")]
    public string Action { get; set; } = string.Empty;

    [Column(StringLength = 255), Description("绑定的网关接口路径，支持 /xxx/** 前缀通配")]
    public string InterfacePath { get; set; } = string.Empty;

    [Description("授权层面：1平台，2商户，3两者")]
    public int AllowedScopes { get; set; } = 3;

    [Description("是否启用")]
public bool IsEnabled { get; set; } = true;
}
