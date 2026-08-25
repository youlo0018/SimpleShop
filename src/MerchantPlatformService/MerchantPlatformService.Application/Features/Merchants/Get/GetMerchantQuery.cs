using MediatR;

namespace MerchantPlatformService.Application.Features.Merchants.Get;

public sealed record GetMerchantQuery(long Id) : IRequest<object>;
