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
    Task<Order?> GetByOrderNoAsync(string orderNo, CancellationToken cancellationToken = default);
    Task<bool> HasIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task<List<Order>> QueryExpiredAwaitPaymentAsync(DateTime now, int limit, CancellationToken cancellationToken = default);
}


