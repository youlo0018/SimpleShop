using MediatR;

namespace CartService.Application.Features.Cart.Add;

public sealed record AddCartItemCommand : IRequest<object>
{
    /// <summary>用户 ID（网关登录态注入）。</summary>
    public long UserId { get; init; }
    /// <summary>商品 ID。</summary>
    public long ProductId { get; init; }
    /// <summary>商品 SKU ID。</summary>
    public long SkuId { get; init; }
    /// <summary>商户 ID。</summary>
    public long MerchantId { get; init; }
    /// <summary>平台 ID。</summary>
    public long PlatformId { get; init; }
    /// <summary>商品名称快照。</summary>
    public string ProductName { get; init; } = string.Empty;
    /// <summary>图片地址。</summary>
    public string Image { get; init; } = string.Empty;
    /// <summary>单价（元）。</summary>
    public decimal Price { get; init; }
    /// <summary>数量。</summary>
    public int Quantity { get; init; } = 1;
}
