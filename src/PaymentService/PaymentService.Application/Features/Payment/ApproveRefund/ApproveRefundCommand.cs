using CommunalService.Domain;
using MediatR;

namespace PaymentService.Application.Features.Payment.ApproveRefund;

/// <summary>同意退款（可选原因；通过后发 payment.refunded 事件）。</summary>
/// <param name="Id">主键。</param>
/// <param name="Reason">原因。</param>
public record ApproveRefundCommand(long Id, string? Reason = null) : IRequest<ApiResponse>;
