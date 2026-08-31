using CommunalService.Domain;
using MediatR;

namespace OrderService.Application.Features.GetOrderList;

public record ListOrdersQuery(
    string Keyword = "", int? Status = null, long CustomerId = 0,
    int Page = 1, int PageSize = 10) : IRequest<ApiResponse>;
