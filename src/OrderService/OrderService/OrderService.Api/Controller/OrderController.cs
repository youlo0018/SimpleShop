using CommunalService.Domain;
using CommunalService.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application.Features.CancelOrder;
using OrderService.Application.Features.CreateOrder;
using OrderService.Application.Features.CreateShipment;
using OrderService.Application.Features.ReceiveShipment;
using OrderService.Application.Features.GetOrder;
using OrderService.Application.Features.GetOrderDetail;
using OrderService.Application.Features.GetOrderList;




namespace OrderService.Api.Controller;


public class OrderController(IMediator mediator, TenantContext tenant) : BaseController
{
    [HttpGet]
    public Task<ApiResponse> List([FromQuery] ListOrdersQuery query)
        => mediator.Send(query, CancellationToken.None);

    [HttpPost]
    public async Task<ApiResponse> Create([FromBody] CreateOrderCommand command)
    {
        if (tenant.UserId <= 0) return Error(BaseApiResponseCode.Unauthorized, "请先登录");

        // 用户下单只允许给自己下单；后台如未来需要代客下单，应单独使用受控的客服能力。
        command = command with { CustomerId = tenant.UserId };
        return Ok(await mediator.Send(command, CancellationToken.None));
    }

    [HttpGet]
    public async Task<ApiResponse> Get([FromQuery] GetOrderQuery query)
    {
        if (tenant.IsCustomer) query = query with { CustomerId = tenant.UserId };
        var order = await mediator.Send(query, CancellationToken.None);
        return order is null ? Error(BaseApiResponseCode.NotFound, "订单不存在") : Ok(order);
    }

    [HttpGet]
    public async Task<ApiResponse> Detail([FromQuery] GetOrderDetailQuery query)
    {
        query = query with
        {
            PlatformId = tenant.IsPlatform ? tenant.PlatformId : query.PlatformId,
            MerchantId = tenant.IsMerchant ? tenant.MerchantId : query.MerchantId,
            CustomerId = tenant.IsCustomer ? tenant.UserId : query.CustomerId
        };
        return Ok(await mediator.Send(query, CancellationToken.None));
    }

    [HttpPost]
    public Task<ApiResponse> Shipment([FromBody] CreateShipmentCommand command)
        => mediator.Send(command, CancellationToken.None);
    [HttpPost]
    public async Task<ApiResponse> Receive([FromBody] ReceiveShipmentCommand command)
    {
        command = command with { CustomerId = tenant.UserId, OverrideOwnerCheck = !tenant.IsCustomer };
        return Ok(await mediator.Send(command, CancellationToken.None));
    }

    [HttpPost]
    public async Task<ApiResponse> Cancel([FromBody] CancelOrderCommand command)
    {
        // 与确认收货一致：客户只能取消本人订单，后台按租户范围；Override 由网关可信身份决定，禁止请求体伪造。
        // Handler 已返回完整 ApiResponse：直接返回，禁止再包 Ok()（会形成双层信封，前端读不到 success/code）。
        command = command with { CustomerId = tenant.UserId, OverrideCustomerScope = !tenant.IsCustomer };
        return await mediator.Send(command, CancellationToken.None);
    }
}
