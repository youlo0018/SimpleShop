using System.ComponentModel;
using CommunalService.Domain.Entity;
using FreeSql.DataAnnotations;

namespace PaymentService.Domain.Entity;

/// <summary>
/// 退款明细：审批通过后按 SKU 恢复库存，并作为后台详情页的业务凭据。
/// </summary>
[Table(Name = "refund_order_item")]
public sealed class RefundOrderItem : BaseEntity
{
    [Description("退款单ID")] public long RefundId { get; set; }

    [Description("业务订单号")] public string BizNo { get; set; } = string.Empty;

    [Description("商品ID")] public long ProductId { get; set; }

    [Description("SKU ID")] public long SkuId { get; set; }

    [Column(StringLength = 80), Description("商品名称")] public string ProductName { get; set; } = string.Empty;

    [Column(Precision = 18, Scale = 2), Description("单价")] public decimal Price { get; set; }

    [Description("数量")] public int Quantity { get; set; }
}
