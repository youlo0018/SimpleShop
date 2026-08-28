using System.ComponentModel;
using CommunalService.Domain.Entity;
using FreeSql.DataAnnotations;

namespace UserService.Domain.Entity;

[Table(Name = "user_address")]
public sealed class Address : BaseEntity
{
    [Description("用户ID")] public long UserId { get; set; }

    [Column(StringLength = 32), Description("收货人")]
    public string ReceiverName { get; set; } = string.Empty;

    [Column(StringLength = 20), Description("联系电话")]
    public string ReceiverPhone { get; set; } = string.Empty;

    [Column(StringLength = 64), Description("省份")]
    public string Province { get; set; } = string.Empty;

    [Column(StringLength = 64), Description("城市")]
    public string City { get; set; } = string.Empty;

    [Column(StringLength = 64), Description("区县")]
    public string District { get; set; } = string.Empty;

    [Column(StringLength = 255), Description("详细地址")]
    public string Detail { get; set; } = string.Empty;

    [Description("是否默认地址")] public bool IsDefault { get; set; }
}
