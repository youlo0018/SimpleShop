using System.ComponentModel;
using CommunalService.Domain.Entity;
using FreeSql.DataAnnotations;

namespace OrderService.Domain.Entity;

/// <summary>
/// 订单明细：保存下单那一刻的商品名、价格和归属关系。
/// 商品以后改名、改价、换商户，都不能影响用户手里这张“凭证”。
/// </summary>
[Table(Name = "order_item")]
[Index("uk_order_sku", "OrderId")]
public sealed class OrderItem : BaseEntity
{
    [Description("平台ID")] public long PlatformId { get; set; }

    [Description("商户ID")] public long MerchantId { get; set; }

    [Description("订单ID")] public long OrderId { get; set; }

    [Description("商品ID")] public long ProductId { get; set; }

    [Description("SKU ID")] public long SkuId { get; set; }

    [Column(StringLength = 128), Description("商品名快照")]
    /// <summary>商品名快照</summary>
    public string ProductName { get; set; } = string.Empty;

    [Description("成交单价")] public decimal Price { get; set; }

    [Description("购买数量")] public int Quantity { get; set; }

    [Description("本行优惠金额（活动或券）")] public decimal DiscountAmount { get; set; }

    [Description("命中营销类型 0无 1活动 2券")] public int MarketingType { get; set; }

    [Description("活动ID或用户券ID")] public long MarketingId { get; set; }

    [Column(StringLength = 64), Description("营销名称快照")]
    /// <summary>营销名称快照</summary>
    public string MarketingName { get; set; } = string.Empty;

    [Description("使用的用户券ID")] public long UserCouponId { get; set; }

    [Description("小计金额")] public decimal SubtotalAmount => Price * Quantity;
}

