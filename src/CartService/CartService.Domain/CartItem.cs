using System.ComponentModel;
using CommunalService.Domain.Entity;
using FreeSql.DataAnnotations;

namespace CartService.Domain;

/// <summary>
/// 购物车条目必须持久化到用户所属商城库，Redis 只保留给锁与缓存；这里沿用全局软删除约定。
/// </summary>
[Table(Name = "cart_items")]
public sealed class CartItem : BaseEntity
{
    /// <summary>商品 SKU ID。</summary>
    public long SkuId { get; set; }
    /// <summary>用户 ID（网关登录态注入）。</summary>
    public long UserId { get; set; }

    /// <summary>商品 ID。</summary>
    public long ProductId { get; set; }
    /// <summary>商户 ID。</summary>
    public long MerchantId { get; set; }
    /// <summary>平台 ID。</summary>
    public long PlatformId { get; set; }
    /// <summary>商品名称快照。</summary>
    public string ProductName { get; set; } = string.Empty;
    [Column(StringLength = 255), Description("商品图片快照")]
    /// <summary>商品图片快照</summary>
    public string Image { get; set; } = string.Empty;
    /// <summary>单价（元）。</summary>
    public decimal Price { get; set; }
    /// <summary>数量。</summary>
    public int Quantity { get; set; }
    /// <summary>是否勾选（结算用，默认勾选）。</summary>
    public bool Checked { get; set; } = true;
    /// <summary>加入购物车时间。</summary>
    public DateTime AddedAt { get; set; } = DateTime.Now;
}
