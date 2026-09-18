using CartService.Application.Features.Cart.Add;
using CartService.Application.Features.Cart.Get;
using CartService.Application.Features.Cart.Remove;
using CommunalService.Domain;
using CommunalService.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CartService.Api.Controllers;

/// <summary>购物车入口：控制器只做协议转换；UserId 强制取登录态（网关 X-Claim-UserId），登录校验在 Handler。</summary>
    public class CartController(IMediator mediator, TenantContext tenant) : BaseController
{
    [HttpPost]
    /// <summary>加入购物车（POST，累加数量语义）：UserId 由登录态注入，防替他人操作。</summary>
    public async Task<ApiResponse> Add([FromBody] AddCartItemCommand command)
    {
        command = command with { UserId = tenant.UserId };
        return Ok(await mediator.Send(command, CancellationToken.None));
    }

    [HttpGet]
    /// <summary>购物车列表（GET）：只返回当前登录用户的条目。</summary>
    public async Task<ApiResponse> Get([FromQuery] GetCartQuery query)
    {
        query = query with { UserId = tenant.UserId };
        return Ok(await mediator.Send(query, CancellationToken.None));
    }

    [HttpPost]
    /// <summary>删除购物车条目（POST，软删除）：只允许删除本人条目。</summary>
    public async Task<ApiResponse> Remove([FromBody] RemoveCartItemCommand command)
    {
        command = command with { UserId = tenant.UserId };
        return Ok(await mediator.Send(command, CancellationToken.None));
    }
}
