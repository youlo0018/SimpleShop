using CommunalService.Domain.Infrastructure.Locks;
using MediatR;
using OrderService.Domain.IRepository;

namespace OrderService.Application.Features.CancelOrder;

public sealed class CancelOrderHandler(IOrderRepository repository, IDistributedLock distributedLock)
    : IRequestHandler<CancelOrderCommand, object>
{
    public async Task<object> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await repository.QueryByIdAsync(request.Id);
        if (order is null || (!request.OverrideCustomerScope && order.CustomerId != request.CustomerId))
        {
            return new { success = false, message = "订单不存在" };
        }

        if (!order.CanCancel)
        {
            return new { success = false, message = "当前订单状态不可取消" };
        }

        await using var lockHandle = await distributedLock.AcquireAsync(
            $"lock:order:{order.Id}",
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(2),
            cancellationToken);

        if (lockHandle is null)
        {
            return new { success = false, message = "订单处理中，请稍后重试" };
        }

        // 条件更新是数据库层兜底：即使锁过期，也不会把已支付或已关闭订单改回取消。
        var cancelled = await repository.TryCancelAsync(
            order.Id,
            request.CustomerId,
            request.Reason,
            requireCustomerId: !request.OverrideCustomerScope,
            cancellationToken);

        return cancelled
            ? new { success = true, orderId = order.Id, status = (int)Domain.Entity.OrderState.Cancelled }
            : new { success = false, message = "当前订单状态不可取消" };
    }
}
