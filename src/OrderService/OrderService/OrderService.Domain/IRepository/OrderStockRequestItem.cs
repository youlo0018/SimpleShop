namespace OrderService.Domain.IRepository;

public sealed record OrderStockRequestItem
{
    public long SkuId { get; init; }
    public int Quantity { get; init; }
}
