using MediatR;

namespace MerchantPlatformService.Application.Features.PlatformConfigs.Save;

public sealed record SavePlatformConfigCommand : IRequest<object>
{
    public string ConfigKey { get; init; } = string.Empty;
    public string ConfigValue { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int ConfigType { get; init; }
    public bool IsEnabled { get; init; } = true;
}
