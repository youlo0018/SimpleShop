using System.ComponentModel;
using CommunalService.Domain.Entity;
using FreeSql.DataAnnotations;

namespace OrderService.Domain.Entity;

/// <summary>
/// 发货单：一个已支付订单可以按包裹多次发货；每次发货记录物流和状态。
/// </summary>
[Table(Name = "shipment")]
[Index("uk_shipment_no", "ShipmentNo", true)]
public sealed class Shipment : BaseEntity
{
    [Description("平台ID")] public long PlatformId { get; set; }

    [Description("商户ID")] public long MerchantId { get; set; }

    [Description("订单ID")] public long OrderId { get; set; }

    [Column(StringLength = 32), Description("发货单号")]
    /// <summary>发货单号</summary>
    public string ShipmentNo { get; set; } = string.Empty;

    [Column(StringLength = 64, IsNullable = true), Description("物流公司")]
    /// <summary>物流公司</summary>
    public string LogisticsCompany { get; set; } = string.Empty;

    [Column(StringLength = 64, IsNullable = true), Description("物流单号")]
    /// <summary>物流单号</summary>
    public string TrackingNo { get; set; } = string.Empty;

    [Description("状态：10待发货，20已发货，50已签收，90已取消")]
    /// <summary>状态：10待发货，20已发货，50已签收，90已取消</summary>
    public int Status { get; set; } = 10;

    [Column(IsNullable = true), Description("发货时间")]
    /// <summary>发货时间</summary>
    public DateTime? ShippedAt { get; set; }

    [Column(IsNullable = true), Description("签收时间")]
    /// <summary>签收时间。</summary>
    public DateTime? ReceivedAt { get; set; }
}

/// <summary>
/// 发货明细：记录这个包裹里发了哪些订单项、各多少件，支持部分发货。
/// </summary>
[Table(Name = "shipment_item")]
public sealed class ShipmentItem : BaseEntity
{
    [Description("发货单ID")] public long ShipmentId { get; set; }

    [Description("订单项ID")] public long OrderItemId { get; set; }

    [Description("SKU ID")] public long SkuId { get; set; }

    [Description("发货数量")] public int Quantity { get; set; }

    [Column(StringLength = 255, IsNullable = true), Description("备注")]
    /// <summary>备注</summary>
    public string Remark { get; set; } = string.Empty;
}
