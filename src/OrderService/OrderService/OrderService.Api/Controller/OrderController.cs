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
using FreeSql;
using OrderService.Domain.Entity;




namespace OrderService.Api.Controller;


public sealed record OrderListQuery(
    string Keyword = "", int? Status = null, long CustomerId = 0,
    int Page = 1, int PageSize = 10);

public class OrderController(IMediator mediator, IFreeSql freeSql, TenantContext tenant) : BaseController
{
    [HttpGet]
    public async Task<ApiResponse> List([FromQuery] OrderListQuery query)
    {
        var selection = freeSql.Select<Order>()
            .Where(order => !order.IsDeleted)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Keyword), order =>
                order.OrderNo.Contains(query.Keyword) || order.ReceiverName.Contains(query.Keyword) || order.ReceiverPhone.Contains(query.Keyword))
            .WhereIf(query.Status.HasValue, order => order.OrderStatus == query.Status!.Value)
            .WhereIf(query.CustomerId > 0, order => order.CustomerId == query.CustomerId);
        if (tenant.IsPlatform) selection = selection.Where(order => order.PlatformId == tenant.PlatformId);
        if (tenant.IsMerchant) selection = selection.Where(order => order.MerchantId == tenant.MerchantId);
        if (tenant.IsCustomer) selection = selection.Where(order => order.CustomerId == tenant.UserId);
        if (!tenant.HasWildcard && !tenant.IsPlatform && !tenant.IsMerchant && !tenant.IsCustomer)
            return Error(BaseApiResponseCode.Unauthorized, "请先登录");

        var total = await selection.CountAsync();
        var items = await selection.OrderByDescending(order => order.CreatedAt)
            .Page((Math.Max(query.Page, 1) - 1) * query.PageSize, query.PageSize)
            .ToListAsync();
        return Ok(new { items, total, page = query.Page, pageSize = query.PageSize });
    }

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
    public async Task<ApiResponse> Shipment([FromBody] CreateShipmentCommand command)
    {
        var order = await SelectScopedOrder(command.OrderId);
        if (order is null) return Error(BaseApiResponseCode.Forbidden, "无权操作该订单");
        command = command with { PlatformId = order.PlatformId, MerchantId = order.MerchantId };
        return Ok(await mediator.Send(command, CancellationToken.None));
    }

    private async Task<Order?> SelectScopedOrder(long id) => await freeSql.Select<Order>()
        .Where(order => order.Id == id && !order.IsDeleted)
        .WhereIf(tenant.IsPlatform, order => order.PlatformId == tenant.PlatformId)
        .WhereIf(tenant.IsMerchant, order => order.MerchantId == tenant.MerchantId)
        .FirstAsync();
    [HttpPost]
    public async Task<ApiResponse> Receive([FromBody] ReceiveShipmentCommand command)
    {
        command = command with { CustomerId = tenant.UserId, OverrideOwnerCheck = !tenant.IsCustomer };
        return Ok(await mediator.Send(command, CancellationToken.None));
    }

    [HttpPost]
    public async Task<ApiResponse> Cancel([FromBody] CancelOrderCommand command)
    {
        var order = await SelectScopedOrder(command.Id);
        if (order is null) return Error(BaseApiResponseCode.Forbidden, "无权操作该订单");
        command = command with { CustomerId = tenant.UserId, OverrideCustomerScope = !tenant.IsCustomer };
        return Ok(await mediator.Send(command, CancellationToken.None));
    }
}
