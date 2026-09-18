using CommunalService.Domain;
using CustomerService.Application.Features.Address.DeleteAddress;
using CustomerService.Application.Features.Address.ListAddresses;
using CustomerService.Application.Features.Address.SaveAddress;
using CustomerService.Application.Features.Customer.GetProfile;
using CustomerService.Application.Features.Customer.Login;
using CustomerService.Application.Features.Customer.Register;
using CustomerService.Application.Features.Customer.SaveProfile;
using CustomerService.Application.Features.Customer.Token;
using CustomerService.Application.Features.Favorite.ListFavorites;
using CustomerService.Application.Features.Favorite.ToggleFavorite;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CustomerService.Api.Controllers;

/// <summary>
/// C 端客户入口（前台唯一账号域）：注册/登录/资料/地址/收藏。
/// 控制器只做协议转换；客户身份从网关注入的 X-Claim-UserId 获取，不接受请求体伪造。
/// </summary>
public class CustomerController(IMediator mediator) : BaseController
{
    /// <summary>客户注册（POST /gateway/customers/Register，公开）：成功即签发客户 JWT。</summary>
    public Task<ApiResponse> Register([FromBody] RegisterCommand command)
        => mediator.Send(command, CancellationToken.None);

    /// <summary>客户登录（POST /gateway/customers/Login，公开）：签发客户 JWT（tenant_type=customer）。</summary>
    public Task<ApiResponse> Login([FromBody] LoginCommand command)
        => mediator.Send(command, CancellationToken.None);

    /// <summary>客户资料（GET，登录客户本人）。</summary>
    [HttpGet]
    public Task<ApiResponse> Profile()
        => mediator.Send(new GetProfileQuery(), CancellationToken.None);

    /// <summary>刷新令牌（POST，登录客户本人）：临近过期时换发新令牌（Redis 会话续期）。</summary>
    public Task<ApiResponse> RefreshToken()
        => mediator.Send(new RefreshTokenCommand(), CancellationToken.None);

    /// <summary>登出（POST）：删除 Redis 会话，令牌立即失效。</summary>
    public Task<ApiResponse> Logout()
        => mediator.Send(new LogoutCommand(), CancellationToken.None);

    /// <summary>编辑个人资料（POST，登录客户本人）：头像/性别/生日/邮箱/手机号。</summary>
    public Task<ApiResponse> SaveProfile([FromBody] SaveProfileCommand command)
        => mediator.Send(command, CancellationToken.None);

    /// <summary>地址簿（GET，登录客户本人）：默认地址排前。</summary>
    [HttpGet]
    public Task<ApiResponse> Addresses()
        => mediator.Send(new ListAddressesQuery(), CancellationToken.None);

    /// <summary>新增/编辑地址（POST，登录客户本人）：设默认时自动清除其他默认标记。</summary>
    public Task<ApiResponse> SaveAddress([FromBody] SaveAddressCommand command)
        => mediator.Send(command, CancellationToken.None);

    /// <summary>删除地址（POST，登录客户本人，软删除）。</summary>
    public Task<ApiResponse> DeleteAddress([FromBody] DeleteAddressCommand command)
        => mediator.Send(command, CancellationToken.None);

    /// <summary>收藏列表（GET，登录客户本人）：按创建时间倒序。</summary>
    [HttpGet]
    public Task<ApiResponse> Favorites()
        => mediator.Send(new ListFavoritesQuery(), CancellationToken.None);

    /// <summary>收藏/取消收藏（POST，登录客户本人，幂等切换）。</summary>
    public Task<ApiResponse> ToggleFavorite([FromBody] ToggleFavoriteCommand command)
        => mediator.Send(command, CancellationToken.None);
}
