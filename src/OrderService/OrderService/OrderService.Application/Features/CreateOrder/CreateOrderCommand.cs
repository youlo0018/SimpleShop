using MediatR;

namespace OrderService.Application.Features.CreateOrder;

public sealed record CreateOrderItem
{
    public long SkuId { get; init; }
    public long PlatformId { get; init; }
    public long MerchantId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int Quantity { get; init; }
}

public sealed record CreateOrderCommand : IRequest<object>
{
    public string IdempotencyKey { get; init; } = string.Empty;
    public long PlatformId { get; init; }
    public long CustomerId { get; init; }
    public string CustomerNo { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string ReceiverName { get; init; } = string.Empty;
    public string ReceiverPhone { get; init; } = string.Empty;
    public string ReceiverAddress { get; init; } = string.Empty;
    public List<CreateOrderItem> Items { get; init; } = [];
    public List<OrderStockItem> StockItems { get; init; } = [];
}

public sealed record OrderStockItem
{
    public long SkuId { get; init; }
    public int Quantity { get; init; }
}
