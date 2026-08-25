using MediatR;

namespace MerchantPlatformService.Application.Features.Platforms.Get;

public sealed record GetPlatformQuery(long Id) : IRequest<object>;
