using System.ComponentModel;
using CommunalService.Domain.Entity;
using FreeSql.DataAnnotations;

namespace MerchantPlatformService.Domain.Entity;

[Table(Name = "platform")]
[Index("uk_platform_code", "PlatformCode", IsUnique = true)]
public sealed class Platform : BaseEntity
{
    [Column(StringLength = 32), Description("平台编码")]
    /// <summary>平台编码</summary>
    public string PlatformCode { get; set; } = string.Empty;

    [Column(StringLength = 64), Description("平台名称")]
    /// <summary>平台名称</summary>
    public string PlatformName { get; set; } = string.Empty;

    [Column(StringLength = 128), Description("联系邮箱")]
    /// <summary>联系邮箱</summary>
    public string ContactEmail { get; set; } = string.Empty;

    [Description("是否启用")]
    /// <summary>是否启用</summary>
    public bool IsEnabled { get; set; } = true;

    [Column(Precision = 18, Scale = 2), Description("默认佣金费率")]
    /// <summary>默认佣金费率</summary>
    public decimal DefaultCommissionRate { get; set; }
}
