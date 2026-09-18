using CommunalService.Domain;
using MediatR;

namespace PaymentService.Application.Features.Payment.RejectRefund;

/// <summary>拒绝退款（需原因；状态机校验在 Handler）。</summary>
/// <param name="Id">主键。</param>
/// <param name="Reason">原因。</param>
public record RejectRefundCommand(long Id, string? Reason = null) : IRequest<ApiResponse>;
