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
    /// <summary>内部处理：Permissions。</summary>
    public Task<ApiResponse> Permissions()
        => mediator.Send(new ListPermissionsQuery(), CancellationToken.None);

    [HttpPost]
    /// <summary>写操作：CreatePermission（副作用与幂等键见调用方约定）。</summary>
    public Task<ApiResponse> CreatePermission([FromBody] CreatePermissionCommand command)
        => mediator.Send(command, CancellationToken.None);

    [HttpGet]
    /// <summary>内部处理：Roles。</summary>
    public Task<ApiResponse> Roles()
        => mediator.Send(new ListRolesQuery(), CancellationToken.None);

    [HttpPost]
    /// <summary>写操作：CreateRole（副作用与幂等键见调用方约定）。</summary>
    public Task<ApiResponse> CreateRole([FromBody] CreateRoleCommand command)
        => mediator.Send(command, CancellationToken.None);

    [HttpPut("~/api/Permission/Roles/{id}/Permissions")]
    /// <summary>写操作：UpdatePermissions（副作用与幂等键见调用方约定）。</summary>
    public Task<ApiResponse> UpdatePermissions([FromRoute] long id, [FromBody] UpdateRolePermissionsCommand command)
        => mediator.Send(command with { Id = id }, CancellationToken.None);

    [HttpGet]
    /// <summary>内部处理：Bindings。</summary>
    public Task<ApiResponse> Bindings()
        => mediator.Send(new ListBindingsQuery(), CancellationToken.None);

    [HttpPost]
    /// <summary>内部处理：Bind。</summary>
    public Task<ApiResponse> Bind([FromBody] BindUserCommand command)
        => mediator.Send(command, CancellationToken.None);
}
