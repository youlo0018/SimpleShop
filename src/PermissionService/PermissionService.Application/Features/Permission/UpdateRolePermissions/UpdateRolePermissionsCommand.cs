using CommunalService.Domain;
using MediatR;

namespace PermissionService.Application.Features.Permission.UpdateRolePermissions;

public record UpdateRolePermissionsCommand(long Id, string Name, string Description, List<string> Permissions)
    : IRequest<ApiResponse>;
