using MediatR;

namespace OrderService.Application.Features.GetOrder;

public sealed record GetOrderQuery(long Id, long CustomerId) : IRequest<object>;
