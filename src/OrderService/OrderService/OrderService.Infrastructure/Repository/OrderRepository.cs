using OrderService.Domain.Entity;
using OrderService.Domain.IRepository;
using OrderService.Infrastructure.ExternalServices;

namespace OrderService.Infrastructure.Repository;

public class OrderRepository(IFreeSql freeSql, InventoryClient inventoryClient) : IOrderRepository
{
    public async Task<bool> LockStockAsync(string orderNo, IReadOnlyCollection<OrderStockRequestItem> items, CancellationToken cancellationToken = default)
    {
        // 库存是外部服务，不能通过仓储事务回滚；这里用 gRPC 补偿动作表达“先锁库存”。
        return await inventoryClient.LockAsync(orderNo, items, cancellationToken);
    }

    public async Task<bool> ReleaseStockAsync(string orderNo, IReadOnlyCollection<OrderStockRequestItem> items, CancellationToken cancellationToken = default)
    {
        return await inventoryClient.ReleaseAsync(orderNo, items, cancellationToken);
    }

    public async Task<Order> QueryByIdAsync(long id)
    {
        return await freeSql.Select<Order>().Where(order => order.Id == id).FirstAsync();
    }

    public async Task<bool> AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        return await freeSql.Insert(order).ExecuteAffrowsAsync(cancellationToken) > 0;
    }

    public async Task<bool> AddItemsAsync(long orderId, IReadOnlyCollection<OrderItem> items, CancellationToken cancellationToken = default)
    {
        foreach (var item in items)
        {
            item.OrderId = orderId;
        }

        // 明细数量通常很小；逐条插入可避免批量插入在不同数据库驱动上的行为差异。
        var affectedRows = 0;
        foreach (var item in items)
        {
            affectedRows += await freeSql.Insert(item).ExecuteAffrowsAsync(cancellationToken) > 0 ? 1 : 0;
        }

        return affectedRows == items.Count;
    }

    public Task<List<OrderItem>> GetItemsAsync(long orderId, CancellationToken cancellationToken = default)
    {
        return freeSql.Select<OrderItem>().Where(item => item.OrderId == orderId).ToListAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        return await freeSql.Update<Order>().SetSource(order).ExecuteAffrowsAsync(cancellationToken) > 0;
    }

    public async Task<bool> TryCloseAsync(long id, string cancelReason, CancellationToken cancellationToken = default)
    {
        return await freeSql.Update<Order>()
            .Where(order => order.Id == id &&
                            order.OrderStatus == (int)OrderState.AwaitPayment &&
                            !order.IsPayment)
            .Set(order => order.OrderStatus, (int)OrderState.Closed)
            .Set(order => order.CancelReason, cancelReason)
            .Set(order => order.UpdatedAt, DateTime.Now)
            .ExecuteAffrowsAsync(cancellationToken) > 0;
    }

    public async Task<bool> TryCancelAsync(long id, long customerId, string cancelReason, bool requireCustomerId = true, CancellationToken cancellationToken = default)
    {
        return await freeSql.Update<Order>()
            .WhereIf(requireCustomerId, order => order.CustomerId == customerId)
            .Where(order => order.Id == id &&
                            order.OrderStatus == (int)OrderState.AwaitPayment &&
                            !order.IsPayment)
            .Set(order => order.OrderStatus, (int)OrderState.Cancelled)
            .Set(order => order.CancelReason, cancelReason)
            .Set(order => order.UpdatedAt, DateTime.Now)
            .ExecuteAffrowsAsync(cancellationToken) > 0;
    }

    public async Task<bool> TryMarkPaidAsync(string orderNo, DateTime paidAt, CancellationToken cancellationToken = default)
    {
        return await freeSql.Update<Order>()
            .Where(order => order.OrderNo == orderNo &&
                            order.OrderStatus == (int)OrderState.AwaitPayment &&
                            !order.IsPayment)
            .Set(order => order.OrderStatus, (int)OrderState.Paid)
            .Set(order => order.IsPayment, true)
            .Set(order => order.PaymentAt, paidAt)
            .ExecuteAffrowsAsync(cancellationToken) > 0;
    }

    public async Task<bool> TryMarkShippedAsync(long id, CancellationToken cancellationToken = default)
    {
        return await freeSql.Update<Order>()
            .Where(order => order.Id == id &&
                            order.OrderStatus == (int)OrderState.Paid &&
                            order.IsPayment)
            .Set(order => order.OrderStatus, (int)OrderState.Shipped)
            .Set(order => order.UpdatedAt, DateTime.Now)
            .ExecuteAffrowsAsync(cancellationToken) > 0;
    }

    public Task<Order?> GetByOrderNoAsync(string orderNo, CancellationToken cancellationToken = default)
    {
        return freeSql.Select<Order>().Where(order => order.OrderNo == orderNo).FirstAsync();
    }

    public async Task<bool> TryApplyRefundAsync(string orderNo, bool isAllRefund, CancellationToken cancellationToken = default)
    {
        // 部分退款只改变退款标记；全额退款才把订单推进到终态，避免覆盖发货中的状态。
        return await freeSql.Update<Order>()
            .Where(order => order.OrderNo == orderNo && order.IsPayment)
            .Set(order => order.IsRefund, true)
            .SetIf(isAllRefund, order => order.IsAllRefund, true)
            .SetIf(isAllRefund, order => order.OrderStatus, (int)OrderState.Refunded)
            .Set(order => order.UpdatedAt, DateTime.Now)
            .ExecuteAffrowsAsync(cancellationToken) > 0;
    }

    public Task<bool> HasIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return freeSql.Select<Order>().AnyAsync(order => order.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task<List<Order>> QueryExpiredAwaitPaymentAsync(DateTime now, int limit, CancellationToken cancellationToken = default)
    {
        return await freeSql.Select<Order>()
            .Where(order => order.OrderStatus == (int)OrderState.AwaitPayment && order.PaymentExpiredAt < now)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<(List<Order> Items, long Total)> QueryPagedAsync(string keyword, int? status, long customerId,
        long? platformId, long? merchantId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var selection = freeSql.Select<Order>()
            .Where(order => !order.IsDeleted)
            .WhereIf(!string.IsNullOrWhiteSpace(keyword), order =>
                order.OrderNo.Contains(keyword) || order.ReceiverName.Contains(keyword) || order.ReceiverPhone.Contains(keyword))
            .WhereIf(status.HasValue, order => order.OrderStatus == status!.Value)
            .WhereIf(customerId > 0, order => order.CustomerId == customerId)
            .WhereIf(platformId.HasValue, order => order.PlatformId == platformId!.Value)
            .WhereIf(merchantId.HasValue, order => order.MerchantId == merchantId!.Value);
        var total = await selection.CountAsync();
        var items = await selection.OrderByDescending(order => order.CreatedAt)
            .Page(Math.Max(page, 1), pageSize).ToListAsync();
        return (items, total);
    }
}
