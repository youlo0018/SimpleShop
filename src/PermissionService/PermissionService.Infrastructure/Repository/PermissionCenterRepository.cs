using FreeSql;
using PermissionService.Domain.Entity;
using PermissionService.Domain.IRepository;

namespace PermissionService.Infrastructure.Repository;

/// <summary>权限中心仓储实现：权限点/角色/角色权限/用户绑定（绑定唯一索引在 UserId 上，重绑需物理删除）。</summary>
    public class PermissionCenterRepository(IFreeSql freeSql) : IPermissionCenterRepository
{
    /// <summary>查询列表（分页语义由实现约定）：ListEnabledPermissionsAsync。</summary>
    /// <summary>取启用的权限点（网关目录缓存用）。</summary>
    public Task<List<Permission>> ListEnabledPermissionsAsync(CancellationToken cancellationToken = default)
        => freeSql.Select<Permission>().Where(item => item.IsEnabled && !item.IsDeleted)
            .OrderBy(item => item.Resource).OrderBy(item => item.Action).ToListAsync();

    /// <summary>查询列表（分页语义由实现约定）：ListAllPermissionsAsync。</summary>
    /// <summary>取全部权限点（后台管理列表用）。</summary>
    public Task<List<Permission>> ListAllPermissionsAsync(CancellationToken cancellationToken = default)
        => freeSql.Select<Permission>().ToListAsync();

    /// <summary>权限编码或接口路径是否已存在（唯一性校验）。</summary>
    public Task<bool> PermissionExistsAsync(string code, string interfacePath, CancellationToken cancellationToken = default)
        => freeSql.Select<Permission>().AnyAsync(item => item.Code == code || item.InterfacePath == interfacePath);

    /// <summary>写入/新增：InsertPermissionAsync。</summary>
    /// <summary>新增权限点。</summary>
    public async Task InsertPermissionAsync(Permission permission, CancellationToken cancellationToken = default)
        => await freeSql.Insert(permission).ExecuteAffrowsAsync(cancellationToken);

    /// <summary>查询列表（分页语义由实现约定）：ListRolesAsync。</summary>
    /// <summary>取角色列表（后台角色下拉与绑定用）。</summary>
    public Task<List<Role>> ListRolesAsync(CancellationToken cancellationToken = default)
        => freeSql.Select<Role>().OrderBy(item => item.TenantType).OrderBy(item => item.Code).ToListAsync();

    /// <summary>查询：GetRoleAsync。</summary>
    /// <summary>按主键取角色。</summary>
    public Task<Role?> GetRoleAsync(long id, CancellationToken cancellationToken = default)
        => freeSql.Select<Role>().Where(item => item.Id == id && !item.IsDeleted).FirstAsync();

    /// <summary>查询：GetRoleByCodeAsync。</summary>
    /// <summary>按编码取角色（登录解析/绑定时定位）。</summary>
    public Task<Role?> GetRoleByCodeAsync(string code, CancellationToken cancellationToken = default)
        => freeSql.Select<Role>().Where(item => item.Code == code && !item.IsDeleted).FirstAsync();

    /// <summary>角色编码是否已存在。</summary>
    public Task<bool> RoleCodeExistsAsync(string code, CancellationToken cancellationToken = default)
        => freeSql.Select<Role>().AnyAsync(item => item.Code == code && !item.IsDeleted);

    /// <summary>写入/新增：InsertRoleAsync。</summary>
    /// <summary>新增角色。</summary>
    public async Task InsertRoleAsync(Role role, CancellationToken cancellationToken = default)
        => await freeSql.Insert(role).ExecuteAffrowsAsync(cancellationToken);

    /// <summary>更新：UpdateRoleAsync。</summary>
    /// <summary>更新角色。</summary>
    public async Task UpdateRoleAsync(Role role, CancellationToken cancellationToken = default)
        => await freeSql.Update<Role>().SetSource(role).ExecuteAffrowsAsync(cancellationToken);

    /// <summary>查询列表（分页语义由实现约定）：ListRolePermissionsAsync。</summary>
    /// <summary>取角色-权限映射（后台回显角色权限）。</summary>
    public Task<List<RolePermission>> ListRolePermissionsAsync(CancellationToken cancellationToken = default)
        => freeSql.Select<RolePermission>().ToListAsync();

    /// <summary>整组替换角色权限：先删旧映射再批量插入。</summary>
    public async Task ReplaceRolePermissionsAsync(long roleId, IEnumerable<string> codes, CancellationToken cancellationToken = default)
    {
        var codeList = codes.ToList();
        await freeSql.Delete<RolePermission>().DisableGlobalFilter("SoftDelete")
            .Where(item => item.RoleId == roleId).ExecuteAffrowsAsync(cancellationToken);
        var ids = (await freeSql.Select<Permission>().Where(item => codeList.Contains(item.Code)).ToListAsync(item => item.Id))
            .Distinct().ToList();
        if (ids.Count > 0)
            await freeSql.Insert(ids.Select(permissionId => new RolePermission { RoleId = roleId, PermissionId = permissionId }).ToList())
                .ExecuteAffrowsAsync(cancellationToken);
    }

    /// <summary>查询列表（分页语义由实现约定）：ListBindingsAsync。</summary>
    /// <summary>取有效用户绑定（排除软删）。</summary>
    public Task<List<UserRole>> ListBindingsAsync(CancellationToken cancellationToken = default)
        => freeSql.Select<UserRole>().Where(item => !item.IsDeleted).ToListAsync();

    /// <summary>查询：GetUserBindingAsync。</summary>
    /// <summary>取用户的主角色绑定（一用户一绑定）。</summary>
    public Task<UserRole?> GetUserBindingAsync(long userId, CancellationToken cancellationToken = default)
        => freeSql.Select<UserRole>().Where(item => item.UserId == userId && !item.IsDeleted).FirstAsync();

    /// <summary>软删除绑定（仅用于展示状态，重绑请用 HardDeleteBindingAsync）。</summary>
    public async Task SoftDeleteBindingAsync(long id, CancellationToken cancellationToken = default)
        => await freeSql.Update<UserRole>().Where(item => item.Id == id)
            .Set(item => item.IsDeleted, true).ExecuteAffrowsAsync(cancellationToken);

    /// <summary>
    /// 物理删除绑定：唯一索引 uk_user_primary_role 建在 UserId 上且不过滤软删，
    /// 重绑前必须物理删除，否则新绑定插入会违反唯一约束。
    /// </summary>
    /// <summary>物理删除绑定：唯一索引不过滤软删，重绑前必须物理删除。</summary>
    public async Task HardDeleteBindingAsync(long id, CancellationToken cancellationToken = default)
        => await freeSql.Delete<UserRole>().Where(item => item.Id == id).ExecuteAffrowsAsync(cancellationToken);

    /// <summary>写入/新增：InsertBindingAsync。</summary>
    /// <summary>新增用户角色绑定。</summary>
    public async Task InsertBindingAsync(UserRole binding, CancellationToken cancellationToken = default)
        => await freeSql.Insert(binding).ExecuteAffrowsAsync(cancellationToken);
}
