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




namespace OrderService.Api.Controller;


public class OrderController(IMediator mediator) : BaseController
{
    [HttpPost]
    public async Task<ApiResponse> Create([FromBody] CreateOrderCommand command)
    {
        return Ok(await mediator.Send(command, CancellationToken.None));
    }

    [HttpGet]
    public async Task<ApiResponse> Get([FromQuery] GetOrderQuery query)
    {
        var order = await mediator.Send(query, CancellationToken.None);
        return order is null ? Error(BaseApiResponseCode.NotFound, "订单不存在") : Ok(order);
    }

    [HttpGet]
    public async Task<ApiResponse> Detail([FromQuery] GetOrderDetailQuery query)
    {
        return Ok(await mediator.Send(query, CancellationToken.None));
    }

    [HttpPost]
    public async Task<ApiResponse> Shipment([FromBody] CreateShipmentCommand command)
    {
        return Ok(await mediator.Send(command, CancellationToken.None));
    }

    [HttpPost]
    public async Task<ApiResponse> Receive([FromBody] ReceiveShipmentCommand command)
    {
        return Ok(await mediator.Send(command, CancellationToken.None));
    }

    [HttpPost]
    public async Task<ApiResponse> Cancel([FromBody] CancelOrderCommand command)
    {
        return Ok(await mediator.Send(command, CancellationToken.None));
    }
}




