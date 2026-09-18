using MediatR;

namespace MerchantPlatformService.Application.Features.Merchants.Get;

/// <summary>商户详情查询（按租户裁剪）。</summary>
/// <param name="Id">主键。</param>
public sealed record GetMerchantQuery(long Id) : IRequest<object>;
