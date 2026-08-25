using System.ComponentModel;
using CommunalService.Domain.Entity;
using FreeSql.DataAnnotations;

namespace MerchantPlatformService.Domain.Entity;

[Table(Name = "platform_config")]
[Index("uk_config_key", "ConfigKey", IsUnique = true)]
public sealed class PlatformConfig : BaseEntity
{
    [Column(StringLength = 64), Description("配置键")]
    public string ConfigKey { get; set; } = string.Empty;

    [Column(StringLength = 500), Description("配置值")]
    public string ConfigValue { get; set; } = string.Empty;

    [Column(StringLength = 255), Description("说明")]
    public string Description { get; set; } = string.Empty;

    [Description("配置类型")]
    public int ConfigType { get; set; }

    [Description("是否启用")]
    public bool IsEnabled { get; set; } = true;
}
