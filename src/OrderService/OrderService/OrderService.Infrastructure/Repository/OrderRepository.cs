using OrderService.Domain.Entity;
using OrderService.Domain.IRepository;
using System.Net.Http;
using System.Net.Http.Json;
using CommunalService.Domain;

namespace OrderService.Infrastructure.Repository;

public class OrderRepository(IFreeSql freeSql) : IOrderRepository
{
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(5) };

    public async Task<bool> LockStockAsync(string orderNo, IReadOnlyCollection<OrderStockRequestItem> items, CancellationToken cancellationToken = default)
    {
        return await SendInventoryAsync("api/Stock/Lock", orderNo, items, cancellationToken);
    }

    public async Task<bool> ReleaseStockAsync(string orderNo, IReadOnlyCollection<OrderStockRequestItem> items, CancellationToken cancellationToken = default)
    {
        return await SendInventoryAsync("api/Stock/Release", orderNo, items, cancellationToken);
    }

    private static async Task<bool> SendInventoryAsync(string url, string orderNo, IReadOnlyCollection<OrderStockRequestItem> items, CancellationToken cancellationToken)
    {
        var response = await HttpClient.PostAsJsonAsync(url, new
        {
            bizNo = orderNo,
            items = items.Select(item => new { item.SkuId, item.Quantity })
        }, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var result = await response.Content.ReadFromJsonAsync<ApiResponse>(cancellationToken: cancellationToken);
        return result?.Code == 200 && result.Data is not null;
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

        return await freeSql.Insert(items).ExecuteAffrowsAsync(cancellationToken) == items.Count;
    }

    public Task<List<OrderItem>> GetItemsAsync(long orderId, CancellationToken cancellationToken = default)
    {
        return freeSql.Select<OrderItem>().Where(item => item.OrderId == orderId).ToListAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        return await freeSql.Update<Order>().SetSource(order).ExecuteAffrowsAsync(cancellationToken) > 0;
    }

    public Task<Order?> GetByOrderNoAsync(string orderNo, CancellationToken cancellationToken = default)
    {
        return freeSql.Select<Order>().Where(order => order.OrderNo == orderNo).FirstAsync();
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
}

