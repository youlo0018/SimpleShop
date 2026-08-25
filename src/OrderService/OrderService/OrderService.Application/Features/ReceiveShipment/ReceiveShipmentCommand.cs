using MediatR;

namespace OrderService.Application.Features.ReceiveShipment;

public sealed record ReceiveShipmentCommand : IRequest<object>
{
    public long ShipmentId { get; init; }
    public long CustomerId { get; set; }
}
