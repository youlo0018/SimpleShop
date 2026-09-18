using CommunalService.Domain.Contracts.Messages;
using CommunalService.Domain.Contracts.Services;
using FreeSql;
using MagicOnion;
using MagicOnion.Server;
using PermissionService.Domain.Entity;

namespace PermissionService.Application.GrpcServices;

/// <summary>权限中心 gRPC：登录解析（Resolve）、角色绑定（AssignRole）、网关权限目录（ListCatalog）。</summary>
public sealed class PermissionGrpcService(IFreeSql freeSql) : ServiceBase<IPermissionService>, IPermissionService
{
    /// <summary>
    /// 登录权限解析：显式角色绑定优先；无绑定且非历史 admin 一律按客户处理（无后台权限）。
    /// 绑定 platform-admin（或历史 admin 无绑定时）授予通配权限 *。
    /// </summary>
    public async UnaryResult<AuthorizationResponse> ResolveAsync(AuthorizationRequest request)
    {
        var binding = await freeSql.Select<UserRole>()
            .Where(item => item.UserId == request.UserId && !item.IsDeleted)
            .FirstAsync();

        // 后台权限只认显式角色绑定：无绑定一律按客户处理（无任何后台权限，fail-closed）。
        if (binding is null)
            return new AuthorizationResponse { Success = true, TenantType = "customer" };

        // 绑定角色必须存在且未软删
        var role = await freeSql.Select<Role>().Where(item => item.Id == binding.RoleId && !item.IsDeleted).FirstAsync();

        if (role is null)
            return new AuthorizationResponse();

        var roleIds = new[] { role.Id };
        // 绑定 platform-admin（或历史 admin 无绑定时）授予通配权限 *。
        var isPlatformAdmin = role.Code == "platform-admin";
        var permissions = isPlatformAdmin
            ? ["*"]
            : await freeSql.Select<RolePermission, Permission>()
                .LeftJoin((mapping, permission) => mapping.PermissionId == permission.Id)
                .Where((mapping, permission) => roleIds.Contains(mapping.RoleId) && permission.IsEnabled)
                .ToListAsync((mapping, permission) => permission.Code);

        return new AuthorizationResponse
        {
            Success = true,
            TenantType = ((TenantType)role.TenantType).ToString().ToLowerInvariant(),
            PlatformId = binding.PlatformId,
            MerchantId = binding.MerchantId,
            Permissions = permissions.Where(code => !string.IsNullOrWhiteSpace(code)).Distinct().ToList(),
            Roles = [role.Code]
        };
    }

    /// <summary>绑定用户角色：一用户一主角色，重绑先物理删除旧绑定再插入（唯一索引约束）。</summary>
    public async UnaryResult<bool> AssignRoleAsync(AssignRoleRequest request)
    {
        var old = await freeSql.Select<UserRole>().Where(item => item.UserId == request.UserId && !item.IsDeleted).FirstAsync();
        if (string.IsNullOrWhiteSpace(request.RoleCode) || request.RoleCode == "customer")
        {
            // 唯一索引建在 UserId 上且不过滤软删，重绑必须物理删除旧绑定，否则插入冲突。
            if (old is not null) await freeSql.Delete<UserRole>().Where(item => item.Id == old.Id).ExecuteAffrowsAsync();
            return true;
        }

        var role = await freeSql.Select<Role>().Where(item => item.Code == request.RoleCode && !item.IsDeleted).FirstAsync();
        if (role is null) return false;
        // 全局超管（platform-admin）允许 PlatformId=0 的跨平台绑定；其他平台角色必须指定平台。
        var isGlobalAdminRole = role.Code == "platform-admin";
        if (role.TenantType == (int)TenantType.Platform && request.PlatformId <= 0 && !isGlobalAdminRole) return false;
        if (role.TenantType == (int)TenantType.Merchant && request.MerchantId <= 0) return false;

        if (old is not null) await freeSql.Delete<UserRole>().Where(item => item.Id == old.Id).ExecuteAffrowsAsync();
        await freeSql.Insert(new UserRole
        {
            UserId = request.UserId, RoleId = role.Id,
            PlatformId = request.PlatformId, MerchantId = request.MerchantId
        }).ExecuteAffrowsAsync();
        return true;
    }

    /// <summary>查询列表（分页语义由实现约定）：ListCatalogAsync。</summary>
    public async UnaryResult<List<PermissionCatalogItem>> ListCatalogAsync()
    {
        return await freeSql.Select<Permission>()
            .Where(item => item.IsEnabled && !item.IsDeleted && !string.IsNullOrWhiteSpace(item.InterfacePath))
            .ToListAsync(item => new PermissionCatalogItem { Code = item.Code, InterfacePath = item.InterfacePath });
    }
}
