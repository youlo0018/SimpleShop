using MediatR;

namespace OrderService.Application.Features.GetOrderDetail;

public sealed record GetOrderDetailQuery(long Id, long CustomerId) : IRequest<object>;
