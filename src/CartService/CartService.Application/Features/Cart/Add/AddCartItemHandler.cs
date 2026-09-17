using CartService.Domain;
using MediatR;

namespace CartService.Application.Features.Cart.Add;

/// <summary>
/// 加入购物车：同一用户+SKU 已存在则**累加** Quantity（调用方增量语义：加购传本次增量、购物车加减传 ±1），
/// 否则新插入；数量累计上限 99，与下单/支付链路一致（购物车已迁移到 PostgreSQL 存储）。
/// </summary>
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
            Image = request.Image,
            Price = request.Price
        };

        if (item.Quantity + request.Quantity > 99)
        {
            return new { success = false, message = "数量不能超过99" };
        }

        // 图片可能后补，每次带图加购时刷新快照，保证购物车/结算页能拿到最新图。
        if (!string.IsNullOrWhiteSpace(request.Image)) item.Image = request.Image;
        item.Quantity += request.Quantity;
        item.Checked = true;
        await cartStore.AddOrUpdateAsync(request.UserId, item, cancellationToken);
        return new { success = true, item.SkuId, item.Quantity };
    }
}
