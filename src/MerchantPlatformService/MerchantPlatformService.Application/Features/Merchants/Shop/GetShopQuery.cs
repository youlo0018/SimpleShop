using MediatR;

namespace MerchantPlatformService.Application.Features.Merchants.Shop;

/// <summary>店铺主页公开信息查询（游客可访问）：只返回已入驻商户的展示字段。</summary>
/// <param name="Id">主键。</param>
public sealed record GetShopQuery(long Id) : IRequest<object>;
