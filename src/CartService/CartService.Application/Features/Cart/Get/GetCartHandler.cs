using CartService.Domain;
using MediatR;

namespace CartService.Application.Features.Cart.Get;

public sealed class GetCartHandler(ICartStore cartStore) : IRequestHandler<GetCartQuery, object>
{
    public async Task<object> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        var items = await cartStore.GetAsync(request.UserId, cancellationToken);
        return new
        {
            items,
            totalQuantity = items.Sum(item => item.Quantity),
            checkedTotalAmount = items.Where(item => item.Checked)
                .Sum(item => item.Price * item.Quantity)
        };
    }
}
