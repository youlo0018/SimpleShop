using MediatR;

namespace MerchantPlatformService.Application.Features.PlatformConfigs.Save;

public sealed record SavePlatformConfigCommand : IRequest<object>
{
    /// <summary>配置键。</summary>
    public string ConfigKey { get; init; } = string.Empty;
    /// <summary>配置值。</summary>
    public string ConfigValue { get; init; } = string.Empty;
    /// <summary>描述。</summary>
    public string Description { get; init; } = string.Empty;
    /// <summary>配置类型。</summary>
    public int ConfigType { get; init; }
    /// <summary>是否启用。</summary>
    public bool IsEnabled { get; init; } = true;
}
