using MediatR;

namespace MerchantPlatformService.Application.Features.PlatformConfigs.Get;

/// <summary>平台配置查询（按配置键）。</summary>
/// <param name="ConfigKey">配置键。</param>
public sealed record GetPlatformConfigQuery(string ConfigKey) : IRequest<object>;
