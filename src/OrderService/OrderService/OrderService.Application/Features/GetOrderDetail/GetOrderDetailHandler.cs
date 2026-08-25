using MediatR;
using OrderService.Domain.IRepository;

namespace OrderService.Application.Features.GetOrderDetail;

/// <summary>
/// 订单详情：主单 + 明细快照一起返回，方便用户核对“当时买了什么”。
/// </summary>
public sealed class GetOrderDetailHandler(IOrderRepository repository)
    : IRequestHandler<GetOrderDetailQuery, object>
{
    public async Task<object> Handle(GetOrderDetailQuery request, CancellationToken cancellationToken)
    {
        var order = await repository.QueryByIdAsync(request.Id);
        if (order is null || order.CustomerId != request.CustomerId)
        {
            return new { success = false, message = "订单不存在" };
        }

        var items = await repository.GetItemsAsync(order.Id, cancellationToken);
        return new
        {
            success = true,
            order = new
            {
                order.Id,
                order.OrderNo,
                order.PlatformId,
                order.OrderStatus,
                order.IsPayment,
                order.TotalPrice,
                order.PaymentPrice,
                order.PaymentAt,
                order.PaymentExpiredAt,
                order.ReceiverName,
                order.ReceiverPhone,
                order.ReceiverAddress
            },
            items
        };
    }
}

