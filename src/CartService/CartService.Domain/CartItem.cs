namespace CartService.Domain;

public sealed class CartItem
{
    public long SkuId { get; init; }
    public long ProductId { get; init; }
    public long MerchantId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int Quantity { get; set; }
    public bool Checked { get; set; } = true;
    public DateTime AddedAt { get; init; } = DateTime.Now;
}
