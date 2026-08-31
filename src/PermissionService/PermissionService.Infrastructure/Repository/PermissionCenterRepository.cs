using FreeSql;
using PermissionService.Domain.Entity;
using PermissionService.Domain.IRepository;

namespace PermissionService.Infrastructure.Repository;

public class PermissionCenterRepository(IFreeSql freeSql) : IPermissionCenterRepository
{
    public Task<List<Permission>> ListEnabledPermissionsAsync(CancellationToken cancellationToken = default)
        => freeSql.Select<Permission>().Where(item => item.IsEnabled && !item.IsDeleted)
            .OrderBy(item => item.Resource).OrderBy(item => item.Action).ToListAsync();

    public Task<List<Permission>> ListAllPermissionsAsync(CancellationToken cancellationToken = default)
        => freeSql.Select<Permission>().ToListAsync();

    public Task<bool> PermissionExistsAsync(string code, string interfacePath, CancellationToken cancellationToken = default)
        => freeSql.Select<Permission>().AnyAsync(item => item.Code == code || item.InterfacePath == interfacePath);

    public async Task InsertPermissionAsync(Permission permission, CancellationToken cancellationToken = default)
        => await freeSql.Insert(permission).ExecuteAffrowsAsync(cancellationToken);

    public Task<List<Role>> ListRolesAsync(CancellationToken cancellationToken = default)
        => freeSql.Select<Role>().OrderBy(item => item.TenantType).OrderBy(item => item.Code).ToListAsync();

    public Task<Role?> GetRoleAsync(long id, CancellationToken cancellationToken = default)
        => freeSql.Select<Role>().Where(item => item.Id == id && !item.IsDeleted).FirstAsync();

    public Task<Role?> GetRoleByCodeAsync(string code, CancellationToken cancellationToken = default)
        => freeSql.Select<Role>().Where(item => item.Code == code && !item.IsDeleted).FirstAsync();

    public Task<bool> RoleCodeExistsAsync(string code, CancellationToken cancellationToken = default)
        => freeSql.Select<Role>().AnyAsync(item => item.Code == code && !item.IsDeleted);

    public async Task InsertRoleAsync(Role role, CancellationToken cancellationToken = default)
        => await freeSql.Insert(role).ExecuteAffrowsAsync(cancellationToken);

    public async Task UpdateRoleAsync(Role role, CancellationToken cancellationToken = default)
        => await freeSql.Update<Role>().SetSource(role).ExecuteAffrowsAsync(cancellationToken);

    public Task<List<RolePermission>> ListRolePermissionsAsync(CancellationToken cancellationToken = default)
        => freeSql.Select<RolePermission>().ToListAsync();

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

    public Task<List<UserRole>> ListBindingsAsync(CancellationToken cancellationToken = default)
        => freeSql.Select<UserRole>().ToListAsync();

    public Task<UserRole?> GetUserBindingAsync(long userId, CancellationToken cancellationToken = default)
        => freeSql.Select<UserRole>().Where(item => item.UserId == userId && !item.IsDeleted).FirstAsync();

    public async Task SoftDeleteBindingAsync(long id, CancellationToken cancellationToken = default)
        => await freeSql.Update<UserRole>().Where(item => item.Id == id)
            .Set(item => item.IsDeleted, true).ExecuteAffrowsAsync(cancellationToken);

    public async Task InsertBindingAsync(UserRole binding, CancellationToken cancellationToken = default)
        => await freeSql.Insert(binding).ExecuteAffrowsAsync(cancellationToken);
}
