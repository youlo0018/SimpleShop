using CommunalService.Domain;
using MediatR;

namespace PermissionService.Application.Features.Permission.CreatePermission;

/// <summary>新增权限点（绑定网关接口路径）。</summary>
public record CreatePermissionCommand(string Code, string Name, string Resource, string Action,
    string InterfacePath, int AllowedScopes = 3) : IRequest<ApiResponse>;
