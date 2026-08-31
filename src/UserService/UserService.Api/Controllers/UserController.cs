using CommunalService.Domain;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.Features.Address.DeleteAddress;
using UserService.Application.Features.Address.ListAddresses;
using UserService.Application.Features.Address.SaveAddress;
using UserService.Application.Features.Favorite.ListFavorites;
using UserService.Application.Features.Favorite.ToggleFavorite;
using UserService.Application.Features.User.CreateUser;
using UserService.Application.Features.User.GetProfile;
using UserService.Application.Features.User.ListUsers;
using UserService.Application.Features.User.Login;
using UserService.Application.Features.User.Register;
using UserService.Application.Features.User.UpdateUser;
using UserService.Application.Features.User.UpdateUserStatus;

using Base = CommunalService.Domain.BaseController;

namespace UserService.Api.Controllers;

/// <summary>
/// 用户/地址/收藏入口：控制器只做协议转换（绑定请求 → Send 命令），
/// 业务逻辑在 Application 层 Handler，输入验证在 FluentValidation Validator，数据访问在 Infrastructure 仓储。
/// </summary>
public class UserController(IMediator mediator) : Base
{
    [HttpGet]
    public IActionResult SyncStructure() => throw new NotImplementedException("表结构由 Program 启动时同步");

    [HttpPost]
    public Task<ApiResponse> Login([FromBody] LoginCommand command)
        => mediator.Send(command, CancellationToken.None);

    [HttpPost]
    public Task<ApiResponse> Register([FromBody] RegisterCommand command)
        => mediator.Send(command, CancellationToken.None);

    [HttpGet]
    public Task<ApiResponse> Profile([FromQuery] GetProfileQuery query)
        => mediator.Send(query, CancellationToken.None);

    [HttpGet]
    public Task<ApiResponse> Users([FromQuery] ListUsersQuery query)
        => mediator.Send(query, CancellationToken.None);

    [HttpPost]
    public Task<ApiResponse> Create([FromBody] CreateUserCommand command)
        => mediator.Send(command, CancellationToken.None);

    [HttpPost]
    public Task<ApiResponse> Update([FromBody] UpdateUserCommand command)
        => mediator.Send(command, CancellationToken.None);

    [HttpPost]
    public Task<ApiResponse> UpdateStatus([FromBody] UpdateUserStatusCommand command)
        => mediator.Send(command, CancellationToken.None);

    [HttpGet]
    public Task<ApiResponse> Addresses([FromQuery] ListAddressesQuery query)
        => mediator.Send(query, CancellationToken.None);

    [HttpPost]
    public Task<ApiResponse> SaveAddress([FromBody] SaveAddressCommand command)
        => mediator.Send(command, CancellationToken.None);

    [HttpPost]
    public Task<ApiResponse> DeleteAddress([FromBody] DeleteAddressCommand command)
        => mediator.Send(command, CancellationToken.None);

    [HttpGet]
    public Task<ApiResponse> Favorites([FromQuery] ListFavoritesQuery query)
        => mediator.Send(query, CancellationToken.None);

    [HttpPost]
    public Task<ApiResponse> ToggleFavorite([FromBody] ToggleFavoriteCommand command)
        => mediator.Send(command, CancellationToken.None);
}
