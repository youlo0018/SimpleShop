using CommunalService.Domain;
using MediatR;

namespace PaymentService.Application.Features.Payment.ListRefunds;

public record ListRefundsQuery(string Keyword = "", int? Status = null, long UserId = 0, int Page = 1, int PageSize = 10)
    : IRequest<ApiResponse>;
