using CommunalService.Domain;
using MediatR;

namespace PaymentService.Application.Features.Payment.ListPayments;

public record ListPaymentsQuery(string Keyword = "", int? Status = null, long UserId = 0, int Page = 1, int PageSize = 10)
    : IRequest<ApiResponse>;
