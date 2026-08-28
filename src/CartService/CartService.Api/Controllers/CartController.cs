using CartService.Application.Features.Cart.Add;
using CartService.Application.Features.Cart.Get;
using CartService.Application.Features.Cart.Remove;
using CommunalService.Domain;
using CommunalService.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CartService.Api.Controllers;

public class CartController(IMediator mediator, TenantContext tenant) : BaseController
{
    [HttpPost]
    public async Task<ApiResponse> Add([FromBody] AddCartItemCommand command)
    {
        if (tenant.UserId <= 0) return Error(BaseApiResponseCode.Unauthorized, "请先登录");
        command = command with { UserId = tenant.UserId };
        return Ok(await mediator.Send(command, CancellationToken.None));
    }

    [HttpGet]
    public async Task<ApiResponse> Get([FromQuery] GetCartQuery query)
    {
        if (tenant.UserId <= 0) return Error(BaseApiResponseCode.Unauthorized, "请先登录");
        query = query with { UserId = tenant.UserId };
        return Ok(await mediator.Send(query, CancellationToken.None));
    }

    [HttpPost]
    public async Task<ApiResponse> Remove([FromBody] RemoveCartItemCommand command)
    {
        if (tenant.UserId <= 0) return Error(BaseApiResponseCode.Unauthorized, "请先登录");
        command = command with { UserId = tenant.UserId };
        return Ok(await mediator.Send(command, CancellationToken.None));
    }
}
