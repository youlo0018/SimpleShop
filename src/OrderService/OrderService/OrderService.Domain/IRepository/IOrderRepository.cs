using CommunalService.Domain.Contracts.Messages;
using OrderService.Domain.Entity;

namespace OrderService.Domain.IRepository;

public interface IOrderRepository
{
    Task<bool> LockStockAsync(string orderNo, IReadOnlyCollection<OrderStockRequestItem> items, CancellationToken cancellationToken = default);
    Task<bool> ReleaseStockAsync(string orderNo, IReadOnlyCollection<OrderStockRequestItem> items, CancellationToken cancellationToken = default);
    Task<Order> QueryByIdAsync(long id);
    Task<bool> AddAsync(Order order, CancellationToken cancellationToken = default);
    Task<bool> AddItemsAsync(long orderId, IReadOnlyCollection<OrderItem> items, CancellationToken cancellationToken = default);
    Task<List<OrderItem>> GetItemsAsync(long orderId, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Order order, CancellationToken cancellationToken = default);

    Task<bool> TryCloseAsync(long id, string cancelReason, CancellationToken cancellationToken = default);

    Task<bool> TryCancelAsync(long id, long customerId, string cancelReason, bool requireCustomerId = true, CancellationToken cancellationToken = default);

    Task<bool> TryMarkPaidAsync(string orderNo, DateTime paidAt, CancellationToken cancellationToken = default);
    Task<bool> TryMarkShippedAsync(long id, CancellationToken cancellationToken = default);
    Task<bool> TryApplyRefundAsync(string orderNo, bool isAllRefund, CancellationToken cancellationToken = default);
    Task<Order?> GetByOrderNoAsync(string orderNo, CancellationToken cancellationToken = default);
    Task<bool> HasIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task<List<Order>> QueryExpiredAwaitPaymentAsync(DateTime now, int limit, CancellationToken cancellationToken = default);
    Task<(List<Order> Items, long Total)> QueryPagedAsync(string keyword, int? status, long customerId,
        long? platformId, long? merchantId, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>营销结算（gRPC）：计算优惠并占用券，失败返回 null（营销服务不可用）。</summary>
    Task<MarketingSettleResponse?> SettleMarketingAsync(MarketingSettleRequest request, CancellationToken cancellationToken = default);

    /// <summary>营销落账（gRPC）：订单落库后写参与/用券记录。</summary>
    Task<bool> CommitMarketingAsync(MarketingCommitRequest request, CancellationToken cancellationToken = default);

    /// <summary>营销回退（gRPC）：下单失败/订单取消时释放券占用。</summary>
    Task ReleaseMarketingAsync(string orderNo, CancellationToken cancellationToken = default);
}
