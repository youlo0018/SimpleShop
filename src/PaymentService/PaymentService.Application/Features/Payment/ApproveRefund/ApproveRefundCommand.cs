using CommunalService.Domain;
using MediatR;

namespace PaymentService.Application.Features.Payment.ApproveRefund;

public record ApproveRefundCommand(long Id, string? Reason = null) : IRequest<ApiResponse>;
