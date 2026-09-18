using OrderService.Domain.Entity;
using OrderService.Domain.IRepository;
using CommunalService.Domain.Contracts.Messages;
using OrderService.Infrastructure.ExternalServices;

namespace OrderService.Infrastructure.Repository;

/// <summary>订单仓储实现：订单/明细落库、状态机条件更新（数据库兜底防非法回退）、库存与营销 gRPC 调用、超时关单扫描。</summary>
    public class OrderRepository(IFreeSql freeSql, InventoryClient inventoryClient, MarketingClient marketingClient) : IOrderRepository
{
    /// <summary>库存操作（幂等键由调用方提供）：LockStockAsync。</summary>
    /// <summary>锁定库存（gRPC 调 InventoryService）：幂等键为订单号，失败返回 false 由下单流程回退券占用。</summary>
    public async Task<bool> LockStockAsync(string orderNo, IReadOnlyCollection<OrderStockRequestItem> items, CancellationToken cancellationToken = default)
    {
        // 库存是外部服务，不能通过仓储事务回滚；这里用 gRPC 补偿动作表达“先锁库存”。
        return await inventoryClient.LockAsync(orderNo, items, cancellationToken);
    }

    /// <summary>库存操作（幂等键由调用方提供）：ReleaseStockAsync。</summary>
    /// <summary>释放库存（gRPC）：取消/关单时调用，幂等键为订单号。</summary>
    public async Task<bool> ReleaseStockAsync(string orderNo, IReadOnlyCollection<OrderStockRequestItem> items, CancellationToken cancellationToken = default)
    {
        return await inventoryClient.ReleaseAsync(orderNo, items, cancellationToken);
    }

    /// <summary>查询：QueryByIdAsync。</summary>
    /// <summary>按主键查询订单；不存在返回 null。</summary>
    public async Task<Order> QueryByIdAsync(long id)
    {
        return await freeSql.Select<Order>().Where(order => order.Id == id).FirstAsync();
    }

    /// <summary>写入/新增：AddAsync。</summary>
    /// <summary>插入订单主表（金额/状态/幂等键由下单 Handler 计算）。</summary>
    public async Task<bool> AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        return await freeSql.Insert(order).ExecuteAffrowsAsync(cancellationToken) > 0;
    }

    /// <summary>写入/新增：AddItemsAsync。</summary>
    /// <summary>批量插入订单明细（含活动/券快照）。</summary>
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

    /// <summary>查询：GetItemsAsync。</summary>
    /// <summary>查询订单明细列表（取消释放库存、发货明细校验用）。</summary>
    public Task<List<OrderItem>> GetItemsAsync(long orderId, CancellationToken cancellationToken = default)
    {
        return freeSql.Select<OrderItem>().Where(item => item.OrderId == orderId).ToListAsync(cancellationToken);
    }

    /// <summary>更新：UpdateAsync。</summary>
    /// <summary>全量更新订单（SetSource，调用方保证字段已按状态机校验）。</summary>
    public async Task<bool> UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        return await freeSql.Update<Order>().SetSource(order).ExecuteAffrowsAsync(cancellationToken) > 0;
    }

    /// <summary>条件关单：仅待支付状态可关闭，数据库条件更新兜底防并发覆盖。</summary>
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

    /// <summary>条件取消：待支付且（可选）校验归属后置为已取消，防非法状态回退。</summary>
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

    /// <summary>条件标记已支付：仅待支付订单可流转（支付回调幂等）。</summary>
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

    /// <summary>条件标记已发货：仅已支付订单可流转。</summary>
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

    /// <summary>查询：GetByOrderNoAsync。</summary>
    /// <summary>按订单号查询（支付/退款回调的幂等键）。</summary>
    public Task<Order?> GetByOrderNoAsync(string orderNo, CancellationToken cancellationToken = default)
    {
        return freeSql.Select<Order>().Where(order => order.OrderNo == orderNo).FirstAsync();
    }

    /// <summary>条件标记退款：部分退款只标记 IsRefund，全额退款置 IsAllRefund。</summary>
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

    /// <summary>幂等键是否已存在（重复下单拦截）。</summary>
    public Task<bool> HasIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return freeSql.Select<Order>().AnyAsync(order => order.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    /// <summary>查询：QueryExpiredAwaitPaymentAsync。</summary>
    /// <summary>扫描超时未支付订单（定时关单用，批量上限由调用方给出）。</summary>
    public async Task<List<Order>> QueryExpiredAwaitPaymentAsync(DateTime now, int limit, CancellationToken cancellationToken = default)
    {
        return await freeSql.Select<Order>()
            .Where(order => order.OrderStatus == (int)OrderState.AwaitPayment && order.PaymentExpiredAt < now)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    /// <summary>后台分页查询：关键字/状态/客户/平台/商户过滤，返回页数据与总数。</summary>
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

    /// <summary>更新：SettleMarketingAsync。</summary>
    /// <summary>调用营销服务结算（gRPC）：计算优惠并占用券；失败返回 null。</summary>
    public async Task<MarketingSettleResponse?> SettleMarketingAsync(MarketingSettleRequest request, CancellationToken cancellationToken = default)
        => await marketingClient.SettleAsync(request, cancellationToken);

    /// <summary>调用营销服务落账（gRPC）：写活动参与/券核销记录（订单号幂等）。</summary>
    public async Task<bool> CommitMarketingAsync(MarketingCommitRequest request, CancellationToken cancellationToken = default)
        => await marketingClient.CommitAsync(request, cancellationToken);

    /// <summary>库存操作（幂等键由调用方提供）：ReleaseMarketingAsync。</summary>
    /// <summary>回退券占用（gRPC，营销侧按订单号幂等）。</summary>
    public async Task ReleaseMarketingAsync(string orderNo, CancellationToken cancellationToken = default)
        => await marketingClient.ReleaseAsync(orderNo, cancellationToken);

    /// <summary>释放库存失败时落补偿表；同订单同 SKU 已有待补偿记录时不重复插入。</summary>
    /// <summary>释放库存失败时写补偿表（ScheduledService 定时重试）。</summary>
    public async Task SavePendingStockReleaseAsync(string orderNo, IReadOnlyCollection<OrderStockRequestItem> items, string error, CancellationToken cancellationToken = default)
    {
        foreach (var item in items.GroupBy(entry => entry.SkuId).Select(group => new { SkuId = group.Key, Quantity = group.Sum(entry => entry.Quantity) }))
        {
            var exists = await freeSql.Select<PendingStockRelease>()
                .AnyAsync(record => record.OrderNo == orderNo && record.SkuId == item.SkuId, cancellationToken);
            if (exists) continue;
            await freeSql.Insert(new PendingStockRelease
            {
                OrderNo = orderNo,
                SkuId = item.SkuId,
                Quantity = item.Quantity,
                LastError = error,
                NextRetryAt = DateTime.Now
            }).ExecuteAffrowsAsync(cancellationToken);
        }
    }
}
