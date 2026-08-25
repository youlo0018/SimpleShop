using MediatR;

namespace PaymentService.Application.Features.CreatePayment;

public sealed record CreatePaymentCommand : IRequest<object>
{
    public string BizNo { get; init; } = string.Empty;
    public long PlatformId { get; init; }
    public long MerchantId { get; init; }
    public long UserId { get; init; }
    public decimal Amount { get; init; }
    public List<PaymentStockItem> Items { get; init; } = [];
}

public sealed record PaymentStockItem
{
    public long SkuId { get; init; }
    public int Quantity { get; init; }
}
