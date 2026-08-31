using CartService.Domain;
using MediatR;

namespace CartService.Application.Features.Cart.Get;

/// <summary>购物车查询：按当前登录用户取全部条目并计算合计；结算时由前端把勾选项带入结算页。</summary>
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
