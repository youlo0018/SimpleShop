using CommunalService.Domain;
using MediatR;

namespace PermissionService.Application.Features.Permission.ListRoles;

public record ListRolesQuery : IRequest<ApiResponse>;
