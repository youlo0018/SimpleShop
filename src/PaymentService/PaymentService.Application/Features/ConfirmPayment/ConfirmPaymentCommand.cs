using CommunalService.Domain;
using MediatR;
using PaymentService.Application.Features.CreatePayment;

namespace PaymentService.Application.Features.ConfirmPayment;

public sealed record ConfirmPaymentCommand : IRequest<ApiResponse>
{
    /// <summary>业务单号（订单号/退款号，幂等键）。</summary>
    public string BizNo { get; init; } = string.Empty;
    /// <summary>明细集合。</summary>
    public List<PaymentStockItem> Items { get; init; } = [];
}
