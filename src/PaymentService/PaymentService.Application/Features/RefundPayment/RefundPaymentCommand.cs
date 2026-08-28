using MediatR;

namespace PaymentService.Application.Features.RefundPayment;

public sealed record RefundPaymentCommand : IRequest<object>
{
    public string BizNo { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Reason { get; init; } = string.Empty;
    public List<RefundStockItem> Items { get; init; } = [];
}

public sealed record RefundStockItem
{
    public long SkuId { get; init; }
    public int Quantity { get; init; }
}
