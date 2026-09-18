using System.ComponentModel;
using CommunalService.Domain.Entity;
using FreeSql.DataAnnotations;

namespace PaymentService.Domain.Entity;

/// <summary>
/// 退款单：支持多次部分退款，累计金额必须由调用方校验，不能超过实付。
/// </summary>
[Table(Name = "refund_order")]
[Index("uk_refund_no", "RefundNo", true)]
public sealed class RefundOrder : BaseEntity
{
    [Column(StringLength = 32), Description("退款单号")]
    /// <summary>退款单号</summary>
    public string RefundNo { get; set; } = string.Empty;

    [Description("支付单ID")] public long PaymentId { get; set; }

    [Column(StringLength = 64), Description("业务订单号")]
    /// <summary>业务订单号</summary>
    public string BizNo { get; set; } = string.Empty;

    [Description("平台ID")] public long PlatformId { get; set; }

    [Description("商户ID")] public long MerchantId { get; set; }

    [Description("用户ID")] public long UserId { get; set; }

    [Column(Precision = 18, Scale = 2), Description("退款金额")]
    /// <summary>金额（元）。</summary>
    public decimal Amount { get; set; }

    [Column(StringLength = 255, IsNullable = true), Description("退款原因")]
    /// <summary>退款原因</summary>
    public string Reason { get; set; } = string.Empty;

    [Description("状态：10待处理，20已退款，90失败")]
    /// <summary>状态：10待处理，20已退款，90失败</summary>
    public int Status { get; set; } = 10;

    [Column(IsNullable = true), Description("退款完成时间")]
    /// <summary>退款完成时间</summary>
    public DateTime? RefundedAt { get; set; }
}
