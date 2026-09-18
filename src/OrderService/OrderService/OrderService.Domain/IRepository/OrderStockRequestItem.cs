namespace OrderService.Domain.IRepository;

public sealed record OrderStockRequestItem
{
    /// <summary>商品 SKU ID。</summary>
    public long SkuId { get; init; }
    /// <summary>数量。</summary>
    public int Quantity { get; init; }
}
