using CommunalService.Domain;
using MediatR;

namespace PermissionService.Application.Features.Permission.UpdateRolePermissions;

/// <summary>整组替换角色权限点（权限中心后台管理）。</summary>
public record UpdateRolePermissionsCommand(long Id, string Name, string Description, List<string> Permissions)
    : IRequest<ApiResponse>;
