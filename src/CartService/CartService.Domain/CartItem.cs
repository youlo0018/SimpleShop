using CommunalService.Domain.Entity;
using FreeSql.DataAnnotations;

namespace CartService.Domain;

/// <summary>
/// 购物车条目必须持久化到用户所属商城库，Redis 只保留给锁与缓存；这里沿用全局软删除约定。
/// </summary>
[Table(Name = "cart_items")]
public sealed class CartItem : BaseEntity
{
    public long SkuId { get; set; }
    public long UserId { get; set; }

    public long ProductId { get; set; }
    public long MerchantId { get; set; }
    public long PlatformId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public bool Checked { get; set; } = true;
    public DateTime AddedAt { get; set; } = DateTime.Now;
}
