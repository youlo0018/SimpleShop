using CartService.Domain;
using MediatR;

namespace CartService.Application.Features.Cart.Remove;

public sealed class RemoveCartItemHandler(ICartStore cartStore) : IRequestHandler<RemoveCartItemCommand, object>
{
    public async Task<object> Handle(RemoveCartItemCommand request, CancellationToken cancellationToken)
    {
        var removed = await cartStore.RemoveAsync(request.UserId, request.SkuId, cancellationToken);
        return new { success = removed };
    }
}
