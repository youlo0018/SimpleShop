using CommunalService.Domain;
using MediatR;
using PaymentService.Application.Features.CreatePayment;

namespace PaymentService.Application.Features.ConfirmPayment;

public sealed record ConfirmPaymentCommand : IRequest<ApiResponse>
{
    public string BizNo { get; init; } = string.Empty;
    public List<PaymentStockItem> Items { get; init; } = [];
}
