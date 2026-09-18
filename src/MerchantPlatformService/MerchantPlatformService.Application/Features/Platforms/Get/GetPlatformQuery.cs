using MediatR;

namespace MerchantPlatformService.Application.Features.Platforms.Get;

/// <summary>平台详情查询。</summary>
/// <param name="Id">主键。</param>
public sealed record GetPlatformQuery(long Id) : IRequest<object>;
