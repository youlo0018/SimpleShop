using MediatR;

namespace OrderService.Application.Features.CreateShipment;

public sealed record ShipmentItemInput
{
    public long OrderItemId { get; init; }
    public long SkuId { get; init; }
    public int Quantity { get; init; }
}

public sealed record CreateShipmentCommand : IRequest<object>
{
    public long OrderId { get; init; }
    public long PlatformId { get; init; }
    public long MerchantId { get; init; }
    public string LogisticsCompany { get; init; } = string.Empty;
    public string TrackingNo { get; init; } = string.Empty;
    public List<ShipmentItemInput> Items { get; init; } = [];
}
