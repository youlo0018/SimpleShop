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
    /// <summary>订单号（幂等与对账键）。</summary>
    public string OrderNo { get; set; }

    [Description("用户id")] public long CustomerId { get; set; }

    [Column(StringLength = 128), Description("用户编号")]
    /// <summary>客户编号（对账/展示用）。</summary>
    public string CustomerNo { get; set; }

    [Column(StringLength = 64), Description("用户名")] public string CustomerName { get; set; }

    [Column(StringLength = 64), Description("收货人")]
    /// <summary>收货人。</summary>
    public string ReceiverName { get; set; }

    [Column(StringLength = 20), Description("收货人电话")]
    /// <summary>收货电话。</summary>
    public string ReceiverPhone { get; set; }

    [Column(StringLength = 255), Description("收货地址快照")]
    /// <summary>收货地址快照。</summary>
    public string ReceiverAddress { get; set; }

    [Description("支付超时时间")]
    /// <summary>支付超时时间</summary>
    public DateTime PaymentExpiredAt { get; set; }

    [Description("取消原因")]
    [Column(StringLength = 255, IsNullable = true)]
    /// <summary>取消原因</summary>
    public string CancelReason { get; set; }

    [Column(StringLength = 64), Description("幂等键")]
    /// <summary>幂等键</summary>
    public string IdempotencyKey { get; set; } = string.Empty;
    [Description("订单总金额")] public decimal TotalPrice { get; set; }
    [Description("优惠总金额")] public decimal AllDiscountPrice { get; set; }
    [Description("支付金额")] public decimal PaymentPrice { get; set; }
    [Description("卡券优惠金额")] public decimal CouponDiscountPrice { get; set; }
    [Description("活动折扣金额")] public decimal ActivityDiscountPrice { get; set; }
    [Description("积分优惠金额")] public decimal PointDiscountPrice { get; set; }
    [Description("订单状态")] public int OrderStatus { get; set; }

    [Description("是否已支付")] public bool IsPayment { get; set; } = false;

    [Column(IsNullable = true), Description("支付时间")]
    /// <summary>支付时间。</summary>
    public DateTime PaymentAt { get; set; }

    [Description("是否有过退款")] public bool IsRefund { get; set; } = false;
    [Description("是否全部退款")] public bool IsAllRefund { get; set; } = false;

    /// <summary>下单时是否成功锁定过库存；取消订单时据此决定是否需要释放（未锁过的不释放，避免虚增可用库存）。</summary>
    [Description("是否锁定过库存")] public bool StockLocked { get; set; } = false;

    /// <summary>支付状态（1 已支付 / 0 未支付）。</summary>
    public int PaymentStatus => IsPayment ? 1 : 0;

    /// <summary>是否可取消（待支付且未支付）。</summary>
    public bool CanCancel => OrderStatus == (int)OrderState.AwaitPayment && !IsPayment;
}
