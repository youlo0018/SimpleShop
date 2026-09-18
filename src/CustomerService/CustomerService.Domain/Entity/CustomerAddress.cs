using System.ComponentModel;
using CommunalService.Domain.Entity;
using FreeSql.DataAnnotations;

namespace CustomerService.Domain.Entity;

/// <summary>客户收货地址簿（C 端私有数据，只允许本人读写）。</summary>
[Table(Name = "customer_address")]
public sealed class CustomerAddress : BaseEntity
{
    [Description("客户ID")] public long CustomerId { get; set; }

    [Column(StringLength = 32), Description("收货人")]
    /// <summary>收货人</summary>
    public string ReceiverName { get; set; } = string.Empty;

    [Column(StringLength = 20), Description("联系电话")]
    /// <summary>联系电话</summary>
    public string ReceiverPhone { get; set; } = string.Empty;

    [Column(StringLength = 64), Description("省份")]
    /// <summary>省份</summary>
    public string Province { get; set; } = string.Empty;

    [Column(StringLength = 64), Description("城市")]
    /// <summary>城市</summary>
    public string City { get; set; } = string.Empty;

    [Column(StringLength = 64), Description("区县")]
    /// <summary>区县</summary>
    public string District { get; set; } = string.Empty;

    [Column(StringLength = 255), Description("详细地址")]
    /// <summary>详细地址</summary>
    public string Detail { get; set; } = string.Empty;

    [Description("是否默认地址")] public bool IsDefault { get; set; }
}
