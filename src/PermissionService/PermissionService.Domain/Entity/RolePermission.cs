using System.ComponentModel;
using CommunalService.Domain.Entity;
using FreeSql.DataAnnotations;

namespace PermissionService.Domain.Entity;

[Table(Name = "role_permission")]
[Index("uk_role_permission", "RoleId,PermissionId", true)]
public sealed class RolePermission : BaseEntity
{
    [Description("角色ID")]
    public long RoleId { get; set; }

    [Description("权限ID")]
    public long PermissionId { get; set; }
}
