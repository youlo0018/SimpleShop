using MediatR;

namespace OrderService.Application.Features.ReceiveShipment;

public sealed record ReceiveShipmentCommand : IRequest<object>
{
    /// <summary>发货单 ID。</summary>
    public long ShipmentId { get; init; }
    /// <summary>客户 ID。</summary>
    public long CustomerId { get; set; }
    /// <summary>是否跳过归属校验（内部调用/管理端专用，默认 false）。</summary>
    public bool OverrideOwnerCheck { get; set; }
}
