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
    /// <summary>平台ID</summary>
    public long PlatformId { get; set; }

    [Column(StringLength = 32), Description("商户编号")]
    /// <summary>商户编号</summary>
    public string MerchantNo { get; set; } = string.Empty;

    [Column(StringLength = 64), Description("商户名称")]
    /// <summary>商户名称</summary>
    public string MerchantName { get; set; } = string.Empty;

    [Column(StringLength = 32), Description("联系人")]
    /// <summary>联系人</summary>
    public string ContactName { get; set; } = string.Empty;

    [Column(StringLength = 20), Description("联系电话")]
    /// <summary>联系电话</summary>
    public string ContactPhone { get; set; } = string.Empty;

    [Column(StringLength = 128), Description("联系邮箱")]
    /// <summary>联系邮箱</summary>
    public string ContactEmail { get; set; } = string.Empty;

    [Description("状态")]
    /// <summary>状态</summary>
    public int Status { get; set; } = (int)MerchantStatus.Draft;

    [Column(Precision = 18, Scale = 2), Description("结算费率")]
    /// <summary>佣金比例（百分比）。</summary>
    public decimal CommissionRate { get; set; }

    [Column(IsNullable = true), Description("审核时间")]
    /// <summary>审核时间。</summary>
    public DateTime? ReviewedAt { get; set; }

    [Column(StringLength = 255, IsNullable = true), Description("拒绝原因")]
    /// <summary>拒绝原因。</summary>
    public string RejectReason { get; set; }

    /// <summary>状态枚举视图（由 Status 转换）。</summary>
    public MerchantStatus State => (MerchantStatus)Status;
}
