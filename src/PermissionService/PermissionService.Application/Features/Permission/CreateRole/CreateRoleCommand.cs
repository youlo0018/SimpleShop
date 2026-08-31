using CommunalService.Domain;
using MediatR;

namespace PermissionService.Application.Features.Permission.CreateRole;

public record CreateRoleCommand(string Code, string Name, int TenantType, string Description, List<string> Permissions)
    : IRequest<ApiResponse>;
