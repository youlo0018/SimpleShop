using CommunalService.Domain;
using MediatR;
using PermissionService.Domain.IRepository;

namespace PermissionService.Application.Features.Permission.ListRoles;

/// <summary>
/// 角色列表：附带每个角色勾选的权限码集合；platform-admin 固定返回 *（全部权限）。
/// </summary>
public class ListRolesQueryHandler(IPermissionCenterRepository repository)
    : IRequestHandler<ListRolesQuery, ApiResponse>
{
    public async Task<ApiResponse> Handle(ListRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await repository.ListRolesAsync(cancellationToken);
        var mappings = await repository.ListRolePermissionsAsync(cancellationToken);
        var permissions = await repository.ListAllPermissionsAsync(cancellationToken);
        var permissionMap = permissions.ToDictionary(item => item.Id, item => item.Code);

        return ApiResults.Ok(roles.Select(role => new
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
}
