using CartService.Domain;
using MediatR;

namespace CartService.Application.Features.Cart.Remove;

/// <summary>移除购物车条目（按用户+SKU 定位删除）。</summary>
public sealed class RemoveCartItemHandler(ICartStore cartStore) : IRequestHandler<RemoveCartItemCommand, object>
{
    /// <summary>处理入口：移除购物车条目（按用户+SKU 定位删除）。</summary>
    public async Task<object> Handle(RemoveCartItemCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId <= 0) return new { success = false, code = 401, message = "请先登录" };
        var removed = await cartStore.RemoveAsync(request.UserId, request.SkuId, cancellationToken);
        return new { success = removed };
    }
}
