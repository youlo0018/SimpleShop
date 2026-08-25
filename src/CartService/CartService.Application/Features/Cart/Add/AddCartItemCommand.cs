using MediatR;

namespace CartService.Application.Features.Cart.Add;

public sealed record AddCartItemCommand : IRequest<object>
{
    public long UserId { get; init; }
    public long ProductId { get; init; }
    public long SkuId { get; init; }
    public long MerchantId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int Quantity { get; init; } = 1;
}
