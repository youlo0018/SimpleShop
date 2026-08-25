using MediatR;

namespace MerchantPlatformService.Application.Features.PlatformConfigs.Get;

public sealed record GetPlatformConfigQuery(string ConfigKey) : IRequest<object>;
