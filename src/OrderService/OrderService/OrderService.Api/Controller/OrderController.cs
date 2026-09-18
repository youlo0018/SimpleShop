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


/// <summary>订单入口：控制器只做协议转换；客户下单强制 CustomerId=登录人，列表/详情按租户裁剪。</summary>
    public class OrderController(IMediator mediator, TenantContext tenant) : BaseController
{
    [HttpGet]
    /// <summary>订单分页列表（GET）：客户只看本人，平台/商户按租户裁剪。</summary>
    public Task<ApiResponse> List([FromQuery] ListOrdersQuery query)
        => mediator.Send(query, CancellationToken.None);

    [HttpPost]
    /// <summary>下单（POST）：客户只能给自己下单，CustomerId 由登录态注入（网关 X-Claim-UserId）。</summary>
    public async Task<ApiResponse> Create([FromBody] CreateOrderCommand command)
    {
        // 用户下单只允许给自己下单；后台如未来需要代客下单，应单独使用受控的客服能力。
        command = command with { CustomerId = tenant.UserId };
        return Ok(await mediator.Send(command, CancellationToken.None));
    }

    [HttpGet]
    /// <summary>订单查询（GET）：客户强制查本人订单。</summary>
    public async Task<ApiResponse> Get([FromQuery] GetOrderQuery query)
    {
        if (tenant.IsCustomer) query = query with { CustomerId = tenant.UserId };
        var order = await mediator.Send(query, CancellationToken.None);
        return order is null ? Error(BaseApiResponseCode.NotFound, "订单不存在") : Ok(order);
    }

    [HttpGet]
    /// <summary>订单详情（GET）：按租户裁剪并返回明细与营销快照。</summary>
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
    /// <summary>发货（POST，order:ship）：商户/平台按租户归属操作。</summary>
    public Task<ApiResponse> Shipment([FromBody] CreateShipmentCommand command)
        => mediator.Send(command, CancellationToken.None);
    [HttpPost]
    /// <summary>确认收货（POST，order:receive）：客户本人操作。</summary>
    public async Task<ApiResponse> Receive([FromBody] ReceiveShipmentCommand command)
    {
        command = command with { CustomerId = tenant.UserId, OverrideOwnerCheck = !tenant.IsCustomer };
        return Ok(await mediator.Send(command, CancellationToken.None));
    }

    [HttpPost]
    /// <summary>取消订单（POST，order:cancel）：待支付可取消并释放库存/回退券。</summary>
    public async Task<ApiResponse> Cancel([FromBody] CancelOrderCommand command)
    {
        // 与确认收货一致：客户只能取消本人订单，后台按租户范围；Override 由网关可信身份决定，禁止请求体伪造。
        // Handler 已返回完整 ApiResponse：直接返回，禁止再包 Ok()（会形成双层信封，前端读不到 success/code）。
        command = command with { CustomerId = tenant.UserId, OverrideCustomerScope = !tenant.IsCustomer };
        return await mediator.Send(command, CancellationToken.None);
    }
}
