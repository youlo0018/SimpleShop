using MediatR;

namespace CartService.Application.Features.Cart.Get;

public sealed record GetCartQuery(long UserId) : IRequest<object>;
