using System.ComponentModel;
using CommunalService.Domain.Entity;
using FreeSql.DataAnnotations;

namespace OrderService.Domin.Entity;

/// <summary>
/// 订单表
/// </summary>
[Index("uk_orderNo", "orderNo", true)]
[Table(Name = "order")]
public sealed class Order : BaseEntity
{
    [Column(StringLength = 32), Description("订单号")]
    public string OrderNo { get; set; }

    [Description("用户id")] public long CustomerId { get; set; }

    [Column(StringLength = 128), Description("用户编号")]
    public string CustomerNo { get; set; }

    [Description("用户名")] public long CustomerName { get; set; }
    [Description("订单总金额")] public decimal TotalPrice { get; set; }
    [Description("优惠总金额")] public decimal AllDiscountPrice { get; set; }
    [Description("支付金额")] public decimal PaymentPrice { get; set; }
    [Description("卡券优惠金额")] public decimal CouponDiscountPrice { get; set; }
    [Description("积分优惠金额")] public decimal PointDiscountPrice { get; set; }
    [Description("订单状态")] public int OrderStatus { get; set; }

    [Description("是否已支付")] public bool IsPayment { get; set; } = false;

    [Column(IsNullable = true), Description("支付时间")]
    public DateTime PaymentAt { get; set; }

    [Description("是否有过退款")] public bool IsRefund { get; set; } = false;
    [Description("是否全部退款")] public bool IsAllRefund { get; set; } = false;
}