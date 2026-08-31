using CommunalService.Domain;
using MediatR;

namespace PermissionService.Application.Features.Permission.ListPermissions;

public record ListPermissionsQuery : IRequest<ApiResponse>;
