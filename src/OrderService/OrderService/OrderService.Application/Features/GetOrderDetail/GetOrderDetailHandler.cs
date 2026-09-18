using MediatR;
using OrderService.Domain.IRepository;

namespace OrderService.Application.Features.GetOrderDetail;

/// <summary>
/// 订单详情：主单 + 明细快照一起返回，方便用户核对“当时买了什么”。
/// </summary>
public sealed class GetOrderDetailHandler(IOrderRepository repository)
    : IRequestHandler<GetOrderDetailQuery, object>
{
    /// <summary>处理入口：订单详情：主单 + 明细快照一起返回，方便用户核对“当时买了什么”。</summary>
    public async Task<object> Handle(GetOrderDetailQuery request, CancellationToken cancellationToken)
    {
        // 后台租户查询时 CustomerId 为 0；只有顾客侧详情才必须校验订单归属。
        var order = await repository.QueryByIdAsync(request.Id);
        if (order is null || (request.CustomerId > 0 && order.CustomerId != request.CustomerId) ||
            (request.PlatformId > 0 && order.PlatformId != request.PlatformId) ||
            (request.MerchantId > 0 && order.MerchantId != request.MerchantId))
        {
            return new { success = false, message = "订单不存在" };
        }

        var items = await repository.GetItemsAsync(order.Id, cancellationToken);
            return new
            {
                success = true,
                order = order,
            items
        };
    }
}
