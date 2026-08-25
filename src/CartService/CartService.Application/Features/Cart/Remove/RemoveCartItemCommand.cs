using MediatR;

namespace CartService.Application.Features.Cart.Remove;

public sealed record RemoveCartItemCommand : IRequest<object>
{
    public long UserId { get; init; }
    public long SkuId { get; init; }
}
