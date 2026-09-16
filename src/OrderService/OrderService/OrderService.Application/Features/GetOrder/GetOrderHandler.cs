using MediatR;
using OrderService.Domain.IRepository;

namespace OrderService.Application.Features.GetOrder;

public sealed class GetOrderHandler(IOrderRepository repository)
    : IRequestHandler<GetOrderQuery, object>
{
    public async Task<object> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        var order = await repository.QueryByIdAsync(request.Id);
        if (order is null || order.CustomerId != request.CustomerId)
        {
            return null;
        }

        return new
        {
            order.Id,
            order.OrderNo,
            order.OrderStatus,
            order.PaymentStatus,
            order.TotalPrice,
            order.AllDiscountPrice,
            order.CouponDiscountPrice,
            order.ActivityDiscountPrice,
            order.PaymentPrice,
            order.IsPayment,
            order.PaymentAt,
            order.PaymentExpiredAt,
            order.IsRefund,
            order.IsAllRefund,
            order.CreatedAt
        };
    }
}
