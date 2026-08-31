using CommunalService.Domain;
using MediatR;

namespace PaymentService.Application.Features.Payment.RejectRefund;

public record RejectRefundCommand(long Id, string? Reason = null) : IRequest<ApiResponse>;
