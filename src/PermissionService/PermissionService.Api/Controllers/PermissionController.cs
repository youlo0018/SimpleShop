using CommunalService.Domain;
using CommunalService.Domain.Entity;
using CommunalService.Domain.Enums;
using FreeSql;
using Microsoft.AspNetCore.Mvc;
using PermissionService.Domain.Entity;

namespace PermissionService.Api.Controllers;

public sealed record SaveRoleRequest(string Code, string Name, int TenantType, string Description, List<string> Permissions);
public sealed record BindUserRequest(long UserId, long RoleId, long PlatformId, long MerchantId);
public sealed record CreatePermissionRequest(string Code, string Name, string Resource, string Action, string InterfacePath, int AllowedScopes = 3);

public class PermissionController(IFreeSql freeSql) : BaseController
{
    [HttpGet]
    public async Task<ApiResponse> Permissions() =>
            Ok(await freeSql.Select<Permission>().Where(item => item.IsEnabled && !item.IsDeleted)
                .OrderBy(item => item.Resource).OrderBy(item => item.Action).ToListAsync());

    [HttpPost]
    public async Task<ApiResponse> CreatePermission([FromBody] CreatePermissionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.InterfacePath))
            return Error(BaseApiResponseCode.BadRequest, "权限编码、名称和接口路径必填");
        if (!request.InterfacePath.StartsWith("/gateway/", StringComparison.OrdinalIgnoreCase))
            return Error(BaseApiResponseCode.BadRequest, "接口路径必须以 /gateway/ 开头");
        if (request.AllowedScopes is not (1 or 2 or 3))
            return Error(BaseApiResponseCode.BadRequest, "权限层面无效");

        var exists = await freeSql.Select<Permission>()
            .AnyAsync(item => item.Code == request.Code || item.InterfacePath == request.InterfacePath);
        if (exists) return Error(BaseApiResponseCode.BadRequest, "权限编码或接口路径已存在");

        var permission = new Permission
        {
            Code = request.Code.Trim(), Name = request.Name.Trim(), Resource = request.Resource,
            Action = request.Action, InterfacePath = request.InterfacePath.Trim(), AllowedScopes = request.AllowedScopes
        };
        await freeSql.Insert(permission).ExecuteAffrowsAsync();
        return Ok(permission);
    }

    [HttpGet]
    public async Task<ApiResponse> Roles()
    {
        var roles = await freeSql.Select<Role>().OrderBy(item => item.TenantType).OrderBy(item => item.Code).ToListAsync();
        var mappings = await freeSql.Select<RolePermission>().ToListAsync();
        var permissions = await freeSql.Select<Permission>().ToListAsync();
        var permissionMap = permissions.ToDictionary(item => item.Id, item => item.Code);
        return Ok(roles.Select(role => new
        {
            role.Id,
            role.Code,
            role.Name,
            role.TenantType,
            role.Description,
            role.IsSystem,
            permissions = role.Code == "platform-admin"
                ? ["*"]
                : mappings.Where(mapping => mapping.RoleId == role.Id)
                .Select(mapping => permissionMap.GetValueOrDefault(mapping.PermissionId, ""))
                    .Where(code => !string.IsNullOrWhiteSpace(code)).ToList()
        }));
    }

    [HttpPost]
    public async Task<ApiResponse> CreateRole([FromBody] SaveRoleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
            return Error(BaseApiResponseCode.BadRequest, "角色编码和名称不能为空");
        if (request.TenantType is not (1 or 2))
            return Error(BaseApiResponseCode.BadRequest, "角色范围无效");
        if (await freeSql.Select<Role>().AnyAsync(item => item.Code == request.Code && !item.IsDeleted))
            return Error(BaseApiResponseCode.BadRequest, "角色编码已存在");

        var role = new Role
        {
            Code = request.Code.Trim(), Name = request.Name.Trim(), TenantType = request.TenantType,
            Description = request.Description ?? string.Empty
        };
        await freeSql.Insert(role).ExecuteAffrowsAsync();
        await ReplacePermissionsAsync(role.Id, request.Permissions);
        return Ok(role);
    }

    [HttpPut("~/api/Permission/Roles/{id}/Permissions")]
    public async Task<ApiResponse> UpdatePermissions([FromRoute] long id, [FromBody] SaveRoleRequest request)
    {
        var role = await freeSql.Select<Role>().Where(item => item.Id == id && !item.IsDeleted).FirstAsync();
        if (role is null) return Error(BaseApiResponseCode.NotFound, "角色不存在");
        role.Name = request.Name; role.Description = request.Description ?? string.Empty;
        await freeSql.Update<Role>().SetSource(role).ExecuteAffrowsAsync();
        await ReplacePermissionsAsync(id, request.Permissions);
        return Ok(new { success = true });
    }

    [HttpGet]
    public async Task<ApiResponse> Bindings()
    {
        var bindings = await freeSql.Select<UserRole>().ToListAsync();
        var roles = await freeSql.Select<Role>().ToListAsync();
        return Ok(bindings.Select(binding => new
        {
            binding.Id, binding.UserId, binding.RoleId,
            roleName = roles.FirstOrDefault(role => role.Id == binding.RoleId)?.Name ?? "",
            roleCode = roles.FirstOrDefault(role => role.Id == binding.RoleId)?.Code ?? "",
            roleTenantType = roles.FirstOrDefault(role => role.Id == binding.RoleId)?.TenantType ?? 0,
            binding.PlatformId, binding.MerchantId, binding.CreatedAt
        }));
    }

    [HttpPost]
    public async Task<ApiResponse> Bind([FromBody] BindUserRequest request)
    {
        if (request.UserId <= 0 || request.RoleId <= 0)
            return Error(BaseApiResponseCode.BadRequest, "用户和角色必选");
        var role = await freeSql.Select<Role>().Where(item => item.Id == request.RoleId && !item.IsDeleted).FirstAsync();
        if (role is null) return Error(BaseApiResponseCode.NotFound, "角色不存在");
        if (role.TenantType == (int)TenantType.Platform && request.PlatformId <= 0)
            return Error(BaseApiResponseCode.BadRequest, "平台角色必须绑定平台");
        if (role.TenantType == (int)TenantType.Merchant && request.MerchantId <= 0)
            return Error(BaseApiResponseCode.BadRequest, "商户角色必须绑定商户");

        var old = await freeSql.Select<UserRole>().Where(item => item.UserId == request.UserId && !item.IsDeleted).FirstAsync();
        if (old is not null)
            await freeSql.Update<UserRole>().Where(item => item.Id == old.Id).Set(item => item.IsDeleted, true).ExecuteAffrowsAsync();

        await freeSql.Insert(new UserRole
        {
            UserId = request.UserId, RoleId = request.RoleId,
            PlatformId = request.PlatformId, MerchantId = request.MerchantId
        }).ExecuteAffrowsAsync();
        return Ok(new { success = true });
    }

    private async Task ReplacePermissionsAsync(long roleId, List<string>? codes)
    {
        codes ??= [];
        // 角色-权限表存在 RoleId + PermissionId 唯一约束；软删除会让同一组合无法重新勾选，必须物理移除旧映射。
        await freeSql.Delete<RolePermission>().DisableGlobalFilter("SoftDelete").Where(item => item.RoleId == roleId).ExecuteAffrowsAsync();

        var ids = (await freeSql.Select<Permission>().Where(item => codes.Contains(item.Code)).ToListAsync(item => item.Id))
            .Distinct().ToList();
        if (ids.Count > 0)
            await freeSql.Insert(ids.Select(permissionId => new RolePermission { RoleId = roleId, PermissionId = permissionId }).ToList())
                .ExecuteAffrowsAsync();
    }
}
