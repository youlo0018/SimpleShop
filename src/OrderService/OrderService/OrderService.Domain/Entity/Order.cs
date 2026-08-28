using System.ComponentModel;
using CommunalService.Domain.Entity;
using FreeSql.DataAnnotations;

namespace OrderService.Domain.Entity;

/// <summary>
/// 订单表
/// </summary>
[Index("uk_orderNo", "orderNo", true)]
[Table(Name = "order")]
public sealed class Order : BaseEntity
{
    [Description("平台ID")] public long PlatformId { get; set; }

    [Description("商户ID")] public long MerchantId { get; set; }

    [Column(StringLength = 32), Description("订单号")]
    public string OrderNo { get; set; }

    [Description("用户id")] public long CustomerId { get; set; }

    [Column(StringLength = 128), Description("用户编号")]
    public string CustomerNo { get; set; }

    [Column(StringLength = 64), Description("用户名")] public string CustomerName { get; set; }

    [Column(StringLength = 64), Description("收货人")]
    public string ReceiverName { get; set; }

    [Column(StringLength = 20), Description("收货人电话")]
    public string ReceiverPhone { get; set; }

    [Column(StringLength = 255), Description("收货地址快照")]
    public string ReceiverAddress { get; set; }

    [Description("支付超时时间")]
    public DateTime PaymentExpiredAt { get; set; }

    [Description("取消原因")]
    [Column(StringLength = 255, IsNullable = true)]
    public string CancelReason { get; set; }

    [Column(StringLength = 64), Description("幂等键")]
    public string IdempotencyKey { get; set; } = string.Empty;
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

    public int PaymentStatus => IsPayment ? 1 : 0;

    public bool CanCancel => OrderStatus == (int)OrderState.AwaitPayment && !IsPayment;
}
