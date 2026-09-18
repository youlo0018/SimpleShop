using CommunalService.Domain;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.Features.User.CreateUser;
using UserService.Application.Features.User.GetProfile;
using UserService.Application.Features.User.ListUsers;
using UserService.Application.Features.User.UpdateUser;
using UserService.Application.Features.User.UpdateUserStatus;

namespace UserService.Api.Controllers;

/// <summary>
/// 后台账号入口（UserService 只服务平台/商户/运营账号；C 端客户走 CustomerService）：
/// 账号管理（列表/建号/改号/启停）与后台资料查询；后台登录由 AuthService 的 OpenIddict 令牌端点完成。
/// </summary>
public class UserController(IMediator mediator) : BaseController
{
    /// <summary>后台账号分页列表（GET，user:read）：keyword 模糊匹配用户名/手机号/邮箱。</summary>
    [HttpGet]
    public Task<ApiResponse> Users([FromQuery] ListUsersQuery query)
        => mediator.Send(query, CancellationToken.None);

    /// <summary>后台建号（POST，user:create）：角色绑定在权限中心，禁止创建客户账号。</summary>
    public Task<ApiResponse> Create([FromBody] CreateUserCommand command)
        => mediator.Send(command, CancellationToken.None);

    /// <summary>后台改号（POST，user:update）：可重置密码并重新绑定角色。</summary>
    public Task<ApiResponse> Update([FromBody] UpdateUserCommand command)
        => mediator.Send(command, CancellationToken.None);

    /// <summary>账号启停（POST，user:update-status）。</summary>
    public Task<ApiResponse> UpdateStatus([FromBody] UpdateUserStatusCommand command)
        => mediator.Send(command, CancellationToken.None);

    /// <summary>后台资料（GET）：wildcard 可查任意 id，否则强制本人。</summary>
    [HttpGet]
    public Task<ApiResponse> Profile([FromQuery] GetProfileQuery query)
        => mediator.Send(query, CancellationToken.None);
}
