using CommunalService.Domain.Infrastructure.Locks;
using MediatR;
using OrderService.Domain.Entity;
using OrderService.Domain.IRepository;

namespace OrderService.Application.Features.CreateShipment;

/// <summary>
/// 商户发货：只有已支付订单能创建发货单；订单锁保证发货和取消/退款不会同时改状态。
/// </summary>
public sealed class CreateShipmentHandler(
    IOrderRepository orderRepository,
    IShipmentRepository shipmentRepository,
    IDistributedLock distributedLock)
    : IRequestHandler<CreateShipmentCommand, object>
{
    public async Task<object> Handle(CreateShipmentCommand request, CancellationToken cancellationToken)
    {
        if (request.Items.Count == 0)
        {
            return new { success = false, message = "发货明细不能为空" };
        }

        var order = await orderRepository.QueryByIdAsync(request.OrderId);
        if (order is null || order.PlatformId != request.PlatformId)
        {
            return new { success = false, message = "订单不存在" };
        }

        if (order.OrderStatus != (int)OrderState.Paid)
        {
            return new { success = false, message = "仅已支付订单可发货" };
        }

        await using var lockHandle = await distributedLock.AcquireAsync(
            $"lock:order:ship:{request.OrderId}",
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(2),
            cancellationToken);

        if (lockHandle is null)
        {
            return new { success = false, message = "订单处理中，请稍后重试" };
        }

        var items = request.Items.Select(item => new ShipmentItem
        {
            OrderItemId = item.OrderItemId,
            SkuId = item.SkuId,
            Quantity = item.Quantity,
            Remark = string.Empty
        }).ToList();

        var shipment = new Shipment
        {
            PlatformId = order.PlatformId,
            MerchantId = request.MerchantId,
            OrderId = order.Id,
            ShipmentNo = $"SH{DateTimeOffset.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}",
            LogisticsCompany = request.LogisticsCompany,
            TrackingNo = request.TrackingNo,
            Status = 20,
            ShippedAt = DateTime.Now
        };

        var added = await shipmentRepository.AddAsync(shipment, items, cancellationToken);
        return added
            ? new { success = true, shipmentNo = shipment.ShipmentNo }
            : new { success = false, message = "发货单创建失败" };
    }
}
