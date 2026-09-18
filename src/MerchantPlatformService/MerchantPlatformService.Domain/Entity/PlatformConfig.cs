using System.ComponentModel;
using CommunalService.Domain.Entity;
using FreeSql.DataAnnotations;

namespace MerchantPlatformService.Domain.Entity;

[Table(Name = "platform_config")]
[Index("uk_config_key", "ConfigKey", IsUnique = true)]
public sealed class PlatformConfig : BaseEntity
{
    [Column(StringLength = 64), Description("配置键")]
    /// <summary>配置键</summary>
    public string ConfigKey { get; set; } = string.Empty;

    [Column(StringLength = 500), Description("配置值")]
    /// <summary>配置值</summary>
    public string ConfigValue { get; set; } = string.Empty;

    [Column(StringLength = 255), Description("说明")]
    /// <summary>说明</summary>
    public string Description { get; set; } = string.Empty;

    [Description("配置类型")]
    /// <summary>配置类型</summary>
    public int ConfigType { get; set; }

    [Description("是否启用")]
    /// <summary>是否启用</summary>
    public bool IsEnabled { get; set; } = true;
}
