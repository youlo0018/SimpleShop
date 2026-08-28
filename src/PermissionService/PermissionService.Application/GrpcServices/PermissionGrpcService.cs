using CommunalService.Domain.Contracts.Messages;
using CommunalService.Domain.Contracts.Services;
using FreeSql;
using MagicOnion;
using MagicOnion.Server;
using PermissionService.Domain.Entity;

namespace PermissionService.Application.GrpcServices;

public sealed class PermissionGrpcService(IFreeSql freeSql) : ServiceBase<IPermissionService>, IPermissionService
{
    public async UnaryResult<AuthorizationResponse> ResolveAsync(AuthorizationRequest request)
    {
        var binding = await freeSql.Select<UserRole>()
            .Where(item => item.UserId == request.UserId && !item.IsDeleted)
            .FirstAsync();

        // 历史后台账号使用 legacy admin；这是唯一通配权限入口，后续新账号一律走显式角色绑定。
        if (binding is null && !string.Equals(request.LegacyRole, "admin", StringComparison.OrdinalIgnoreCase))
            return new AuthorizationResponse { Success = true, TenantType = "customer" };

        var role = binding is null
            ? null
            : await freeSql.Select<Role>().Where(item => item.Id == binding.RoleId && !item.IsDeleted).FirstAsync();

        if (binding is not null && role is null)
            return new AuthorizationResponse();

        var roleIds = role is null ? Array.Empty<long>() : new[] { role.Id };
        var isPlatformAdmin = role?.Code == "platform-admin";
        var permissions = role is null || isPlatformAdmin
            ? ["*"]
            : await freeSql.Select<RolePermission, Permission>()
                .LeftJoin((mapping, permission) => mapping.PermissionId == permission.Id)
                .Where((mapping, permission) => roleIds.Contains(mapping.RoleId) && permission.IsEnabled)
                .ToListAsync((mapping, permission) => permission.Code);

        return new AuthorizationResponse
        {
            Success = true,
            TenantType = role is null ? "platform" : ((TenantType)role.TenantType).ToString().ToLowerInvariant(),
            PlatformId = binding?.PlatformId ?? 0,
            MerchantId = binding?.MerchantId ?? 0,
            Permissions = permissions.Where(code => !string.IsNullOrWhiteSpace(code)).Distinct().ToList(),
            Roles = role is null ? ["platform-admin"] : [role.Code]
        };
    }

    public async UnaryResult<bool> AssignRoleAsync(AssignRoleRequest request)
    {
        var old = await freeSql.Select<UserRole>().Where(item => item.UserId == request.UserId && !item.IsDeleted).FirstAsync();
        if (string.IsNullOrWhiteSpace(request.RoleCode) || request.RoleCode == "customer")
        {
            if (old is not null) await freeSql.Update<UserRole>().Where(item => item.Id == old.Id)
                .Set(item => item.IsDeleted, true).ExecuteAffrowsAsync();
            return true;
        }

        var role = await freeSql.Select<Role>().Where(item => item.Code == request.RoleCode && !item.IsDeleted).FirstAsync();
        if (role is null) return false;
        if (role.TenantType == (int)TenantType.Platform && request.PlatformId <= 0) return false;
        if (role.TenantType == (int)TenantType.Merchant && request.MerchantId <= 0) return false;

        if (old is not null) await freeSql.Update<UserRole>().Where(item => item.Id == old.Id)
            .Set(item => item.IsDeleted, true).ExecuteAffrowsAsync();
        await freeSql.Insert(new UserRole
        {
            UserId = request.UserId, RoleId = role.Id,
            PlatformId = request.PlatformId, MerchantId = request.MerchantId
        }).ExecuteAffrowsAsync();
        return true;
    }

    public async UnaryResult<List<PermissionCatalogItem>> ListCatalogAsync()
    {
        return await freeSql.Select<Permission>()
            .Where(item => item.IsEnabled && !item.IsDeleted && !string.IsNullOrWhiteSpace(item.InterfacePath))
            .ToListAsync(item => new PermissionCatalogItem { Code = item.Code, InterfacePath = item.InterfacePath });
    }
}
