using PermissionService.Domain.Entity;

namespace PermissionService.Domain.IRepository;

/// <summary>权限中心仓储：角色/权限/绑定的查询与写入收口在 Infrastructure。</summary>
public interface IPermissionCenterRepository
{
    Task<List<Permission>> ListEnabledPermissionsAsync(CancellationToken cancellationToken = default);
    Task<List<Permission>> ListAllPermissionsAsync(CancellationToken cancellationToken = default);
    Task<bool> PermissionExistsAsync(string code, string interfacePath, CancellationToken cancellationToken = default);
    Task InsertPermissionAsync(Permission permission, CancellationToken cancellationToken = default);

    Task<List<Role>> ListRolesAsync(CancellationToken cancellationToken = default);
    Task<Role?> GetRoleAsync(long id, CancellationToken cancellationToken = default);
    Task<Role?> GetRoleByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<bool> RoleCodeExistsAsync(string code, CancellationToken cancellationToken = default);
    Task InsertRoleAsync(Role role, CancellationToken cancellationToken = default);
    Task UpdateRoleAsync(Role role, CancellationToken cancellationToken = default);

    Task<List<RolePermission>> ListRolePermissionsAsync(CancellationToken cancellationToken = default);
    /// <summary>角色-权限映射有唯一约束，重绑必须物理移除旧映射（软删会挡住重新勾选）。</summary>
    Task ReplaceRolePermissionsAsync(long roleId, IEnumerable<string> codes, CancellationToken cancellationToken = default);

    Task<List<UserRole>> ListBindingsAsync(CancellationToken cancellationToken = default);
    Task<UserRole?> GetUserBindingAsync(long userId, CancellationToken cancellationToken = default);
    Task SoftDeleteBindingAsync(long id, CancellationToken cancellationToken = default);
    Task InsertBindingAsync(UserRole binding, CancellationToken cancellationToken = default);
}
