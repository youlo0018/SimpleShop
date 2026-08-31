using CommunalService.Domain;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PermissionService.Application.Features.Permission.BindUser;
using PermissionService.Application.Features.Permission.CreatePermission;
using PermissionService.Application.Features.Permission.CreateRole;
using PermissionService.Application.Features.Permission.ListBindings;
using PermissionService.Application.Features.Permission.ListPermissions;
using PermissionService.Application.Features.Permission.ListRoles;
using PermissionService.Application.Features.Permission.UpdateRolePermissions;

namespace PermissionService.Api.Controllers;

/// <summary>角色权限入口：控制器只做协议转换；校验在 Validator，规则在 Handler，读写走仓储。</summary>
public class PermissionController(IMediator mediator) : BaseController
{
    [HttpGet]
    public Task<ApiResponse> Permissions()
        => mediator.Send(new ListPermissionsQuery(), CancellationToken.None);

    [HttpPost]
    public Task<ApiResponse> CreatePermission([FromBody] CreatePermissionCommand command)
        => mediator.Send(command, CancellationToken.None);

    [HttpGet]
    public Task<ApiResponse> Roles()
        => mediator.Send(new ListRolesQuery(), CancellationToken.None);

    [HttpPost]
    public Task<ApiResponse> CreateRole([FromBody] CreateRoleCommand command)
        => mediator.Send(command, CancellationToken.None);

    [HttpPut("~/api/Permission/Roles/{id}/Permissions")]
    public Task<ApiResponse> UpdatePermissions([FromRoute] long id, [FromBody] UpdateRolePermissionsCommand command)
        => mediator.Send(command with { Id = id }, CancellationToken.None);

    [HttpGet]
    public Task<ApiResponse> Bindings()
        => mediator.Send(new ListBindingsQuery(), CancellationToken.None);

    [HttpPost]
    public Task<ApiResponse> Bind([FromBody] BindUserCommand command)
        => mediator.Send(command, CancellationToken.None);
}
