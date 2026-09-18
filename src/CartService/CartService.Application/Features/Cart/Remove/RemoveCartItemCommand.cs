using MediatR;

namespace CartService.Application.Features.Cart.Remove;

public sealed record RemoveCartItemCommand : IRequest<object>
{
    /// <summary>用户 ID（网关登录态注入）。</summary>
    public long UserId { get; init; }
    /// <summary>商品 SKU ID。</summary>
    public long SkuId { get; init; }
}
