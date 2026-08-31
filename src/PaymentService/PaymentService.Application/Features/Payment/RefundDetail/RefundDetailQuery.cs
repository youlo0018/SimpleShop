using CommunalService.Domain;
using MediatR;

namespace PaymentService.Application.Features.Payment.RefundDetail;

public record RefundDetailQuery(long Id) : IRequest<ApiResponse>;
