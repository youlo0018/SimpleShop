using MediatR;
using OrderService.Domain.IRepository;

namespace OrderService.Application.Features.CancelOrder;

public sealed class CancelOrderHandler(IOrderRepository repository)
    : IRequestHandler<CancelOrderCommand, object>
{
    public async Task<object> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await repository.QueryByIdAsync(request.Id);
        if (order is null || order.CustomerId != request.CustomerId)
        {
            return new { success = false, message = "订单不存在" };
        }

        if (!order.CanCancel)
        {
            return new { success = false, message = "当前订单状态不可取消" };
        }

        order.OrderStatus = (int)Domain.Entity.OrderState.Cancelled;
        order.CancelReason = request.Reason;
        await repository.UpdateAsync(order, cancellationToken);
        return new { success = true, orderId = order.Id, status = order.OrderStatus };
    }
}
