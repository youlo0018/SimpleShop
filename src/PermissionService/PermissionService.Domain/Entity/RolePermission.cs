using System.ComponentModel;
using CommunalService.Domain.Entity;
using FreeSql.DataAnnotations;

namespace PermissionService.Domain.Entity;

[Table(Name = "role_permission")]
[Index("uk_role_permission", "RoleId,PermissionId", true)]
public sealed class RolePermission : BaseEntity
{
    [Description("角色ID")]
    /// <summary>角色ID</summary>
    public long RoleId { get; set; }

    [Description("权限ID")]
    /// <summary>权限ID</summary>
    public long PermissionId { get; set; }
}
