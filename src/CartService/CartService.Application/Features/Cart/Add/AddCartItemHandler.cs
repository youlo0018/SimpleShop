using CartService.Domain;
using MediatR;

namespace CartService.Application.Features.Cart.Add;

/// <summary>加入购物车：同一用户+SKU 已存在则累加数量，否则新插入（购物车已迁移到 PostgreSQL 存储）。</summary>
public sealed class AddCartItemHandler(ICartStore cartStore) : IRequestHandler<AddCartItemCommand, object>
{
    public async Task<object> Handle(AddCartItemCommand request, CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0)
        {
            return new { success = false, message = "数量必须大于0" };
        }

        var existing = (await cartStore.GetAsync(request.UserId, cancellationToken))
            .FirstOrDefault(item => item.SkuId == request.SkuId);

        var item = existing ?? new CartItem
        {
            SkuId = request.SkuId,
            ProductId = request.ProductId,
            MerchantId = request.MerchantId,
            PlatformId = request.PlatformId,
            ProductName = request.ProductName,
            Price = request.Price
        };

        item.Quantity += request.Quantity;
        item.Checked = true;
        await cartStore.AddOrUpdateAsync(request.UserId, item, cancellationToken);
        return new { success = true, item.SkuId, item.Quantity };
    }
}
