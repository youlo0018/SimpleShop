using System.ComponentModel;
using CommunalService.Domain.Entity;
using FreeSql.DataAnnotations;

namespace MerchantPlatformService.Domain.Entity;

/// <summary>
/// 每个平台只有一份小程序装修配置；商城端按平台编码拉取，不再共用一套写死的首页。
/// </summary>
[Table(Name = "platform_app_config")]
[Index("uk_platform_app_config_platform", "PlatformId", IsUnique = true)]
public sealed class PlatformAppConfig : BaseEntity
{
    [Description("平台ID")] public long PlatformId { get; set; }

    [Column(StringLength = 64), Description("平台编码")]
    /// <summary>平台编码</summary>
    public string PlatformCode { get; set; } = string.Empty;

    [Column(StringLength = 32768), Description("小程序页面配置JSON")]
    /// <summary>小程序页面配置JSON</summary>
    public string ConfigJson { get; set; } = "{}";

    [Description("发布版本")] public int PublishVersion { get; set; }

    [Description("是否发布")] public bool IsPublished { get; set; }

    [Description("是否启用")] public bool IsEnabled { get; set; } = true;
}
