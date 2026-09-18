using CommunalService.Domain;
using MediatR;

namespace OrderService.Application.Features.GetOrderList;

/// <summary>订单分页查询（客户/平台/商户按租户裁剪）。</summary>
public record ListOrdersQuery(
    string Keyword = "", int? Status = null, long CustomerId = 0,
    int Page = 1, int PageSize = 10) : IRequest<ApiResponse>;
