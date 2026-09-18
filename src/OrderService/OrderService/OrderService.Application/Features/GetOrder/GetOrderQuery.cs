using MediatR;

namespace OrderService.Application.Features.GetOrder;

/// <summary>订单查询（客户强制本人）。</summary>
/// <param name="Id">主键。</param>
/// <param name="CustomerId">客户 ID。</param>
public sealed record GetOrderQuery(long Id, long CustomerId) : IRequest<object>;
