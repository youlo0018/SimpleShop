using CommunalService.Domain;
using MediatR;

namespace PermissionService.Application.Features.Permission.CreatePermission;

public record CreatePermissionCommand(string Code, string Name, string Resource, string Action,
    string InterfacePath, int AllowedScopes = 3) : IRequest<ApiResponse>;
