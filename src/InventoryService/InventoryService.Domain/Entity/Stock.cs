using System.ComponentModel;
using CommunalService.Domain.Entity;
using FreeSql.DataAnnotations;

namespace InventoryService.Domain.Entity;

[Table(Name = "stock")]
[Index("uk_stock_sku", "SkuId", IsUnique = true)]
public sealed class Stock : BaseEntity
{
    [Description("平台ID")] public long PlatformId { get; set; }

    [Description("商户ID")] public long MerchantId { get; set; }

    [Description("SKU ID")] public long SkuId { get; set; }

    [Description("可售库存")] public int AvailableQuantity { get; set; }

    [Description("锁定库存")] public int LockedQuantity { get; set; }

    [Description("已扣减库存")] public int DeductedQuantity { get; set; }

    /// <summary>可售数量 = 总量 - 锁定 - 已扣（计算属性）。</summary>
    public int SellableQuantity => AvailableQuantity - LockedQuantity - DeductedQuantity;
}

[Table(Name = "stock_flow")]
public sealed class StockFlow : BaseEntity
{
    [Description("SKU ID")] public long SkuId { get; set; }

    [Column(StringLength = 64), Description("业务单号")]
    /// <summary>业务单号</summary>
    public string BizNo { get; set; } = string.Empty;

    [Column(StringLength = 32), Description("动作")]
    /// <summary>动作</summary>
    public string Action { get; set; } = string.Empty;

    [Description("数量")] public int Quantity { get; set; }
}
