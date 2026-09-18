using CommunalService.Domain.Infrastructure.Locks;
using MediatR;
using OrderService.Domain.Entity;
using OrderService.Domain.IRepository;

namespace OrderService.Application.Features.ReceiveShipment;

/// <summary>
/// 用户确认收货：包裹置为已签收，并把订单推进到已完成。
/// </summary>
public sealed class ReceiveShipmentHandler(
    IOrderRepository orderRepository,
    IShipmentRepository shipmentRepository,
    IDistributedLock distributedLock)
    : IRequestHandler<ReceiveShipmentCommand, object>
{
    /// <summary>处理入口：用户确认收货：包裹置为已签收，并把订单推进到已完成。</summary>
    public async Task<object> Handle(ReceiveShipmentCommand request, CancellationToken cancellationToken)
    {
        var shipment = await shipmentRepository.GetByIdAsync(request.ShipmentId);
        if (shipment is null)
        {
            return new { success = false, message = "发货单不存在" };
        }

        await using var lockHandle = await distributedLock.AcquireAsync(
            $"lock:order:{shipment.OrderId}",
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(2),
            cancellationToken);

        if (lockHandle is null)
        {
            return new { success = false, message = "签收处理中，请稍后重试" };
        }

        shipment = await shipmentRepository.GetByIdAsync(request.ShipmentId);
        if (shipment is null)
        {
            return new { success = false, message = "订单不存在" };
        }

        var order = await orderRepository.QueryByIdAsync(shipment.OrderId);
        if (order is null ||
            (!request.OverrideOwnerCheck && order.CustomerId != request.CustomerId) ||
            order.OrderStatus != (int)OrderState.Shipped)
        {
            return new { success = false, message = "当前订单状态不可签收" };
        }

        if (shipment.Status != 20)
        {
            return new { success = false, message = "当前包裹状态不可签收" };
        }

        shipment.Status = 50;
        shipment.ReceivedAt = DateTime.Now;
        shipment.UpdatedAt = DateTime.Now;

        var updated = await shipmentRepository.UpdateAsync(shipment, cancellationToken);
        if (!updated)
        {
            return new { success = false, message = "签收失败" };
        }

        order.OrderStatus = (int)OrderState.Completed;
        order.UpdatedAt = DateTime.Now;
        await orderRepository.UpdateAsync(order, cancellationToken);

        return new { success = true, orderId = order.Id, status = order.OrderStatus };
    }
}
