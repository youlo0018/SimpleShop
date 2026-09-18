using MediatR;

namespace OrderService.Application.Features.GetOrderDetail;

/// <summary>订单详情查询（按租户裁剪，含明细与营销快照）。</summary>
/// <param name="Id">主键。</param>
/// <param name="CustomerId">客户 ID。</param>
/// <param name="PlatformId">平台 ID。</param>
/// <param name="MerchantId">商户 ID。</param>
public sealed record GetOrderDetailQuery(long Id, long CustomerId, long PlatformId = 0, long MerchantId = 0) : IRequest<object>;
