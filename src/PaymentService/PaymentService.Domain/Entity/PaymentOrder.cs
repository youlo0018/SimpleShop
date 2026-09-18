using System.ComponentModel;
using CommunalService.Domain.Entity;
using FreeSql.DataAnnotations;

namespace PaymentService.Domain.Entity;

[Table(Name = "payment_order")]
[Index("uk_payment_no", "PaymentNo", IsUnique = true)]
[Index("uk_payment_biz", "BizNo", IsUnique = true)]
public sealed class PaymentOrder : BaseEntity
{
    [Column(StringLength = 32), Description("支付单号")]
    /// <summary>支付单号</summary>
    public string PaymentNo { get; set; } = string.Empty;

    [Column(StringLength = 64), Description("业务单号")]
    /// <summary>业务单号</summary>
    public string BizNo { get; set; } = string.Empty;

    [Description("平台ID")] public long PlatformId { get; set; }

    [Description("商户ID")] public long MerchantId { get; set; }

    [Description("用户ID")] public long UserId { get; set; }

    [Column(Precision = 18, Scale = 2), Description("支付金额")]
    /// <summary>金额（元）。</summary>
    public decimal Amount { get; set; }

    [Description("状态：10待支付，20成功，30失败，40关闭")]
    /// <summary>状态：10待支付，20成功，30失败，40关闭</summary>
    public int Status { get; set; } = 10;

    [Column(IsNullable = true), Description("支付时间")]
    /// <summary>支付时间</summary>
    public DateTime? PaidAt { get; set; }
}
