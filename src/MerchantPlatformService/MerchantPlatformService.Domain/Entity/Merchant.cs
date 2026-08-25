using System.ComponentModel;
using CommunalService.Domain.Entity;
using FreeSql.DataAnnotations;
using MerchantPlatformService.Domain.Enums;

namespace MerchantPlatformService.Domain.Entity;

[Table(Name = "merchant")]
[Index("uk_merchant_no", "MerchantNo", IsUnique = true)]
public sealed class Merchant : BaseEntity
{
    [Description("平台ID")]
    public long PlatformId { get; set; }

    [Column(StringLength = 32), Description("商户编号")]
    public string MerchantNo { get; set; } = string.Empty;

    [Column(StringLength = 64), Description("商户名称")]
    public string MerchantName { get; set; } = string.Empty;

    [Column(StringLength = 32), Description("联系人")]
    public string ContactName { get; set; } = string.Empty;

    [Column(StringLength = 20), Description("联系电话")]
    public string ContactPhone { get; set; } = string.Empty;

    [Column(StringLength = 128), Description("联系邮箱")]
    public string ContactEmail { get; set; } = string.Empty;

    [Description("状态")]
    public int Status { get; set; } = (int)MerchantStatus.Draft;

    [Column(Precision = 18, Scale = 2), Description("结算费率")]
    public decimal CommissionRate { get; set; }

    [Column(IsNullable = true), Description("审核时间")]
    public DateTime? ReviewedAt { get; set; }

    [Column(StringLength = 255, IsNullable = true), Description("拒绝原因")]
    public string RejectReason { get; set; }

    public MerchantStatus State => (MerchantStatus)Status;
}
