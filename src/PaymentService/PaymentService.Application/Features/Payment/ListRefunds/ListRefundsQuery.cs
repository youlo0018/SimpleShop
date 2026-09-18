using CommunalService.Domain;
using MediatR;

namespace PaymentService.Application.Features.Payment.ListRefunds;

/// <summary>退款单分页查询（后台）：关键字/状态/用户过滤。</summary>
/// <param name="Keyword">关键字（模糊匹配）。</param>
/// <param name="Status">状态过滤。</param>
/// <param name="UserId">用户 ID。</param>
/// <param name="Page">页码（从 1 开始）。</param>
/// <param name="PageSize">每页条数。</param>
public record ListRefundsQuery(string Keyword = "", int? Status = null, long UserId = 0, int Page = 1, int PageSize = 10)
    : IRequest<ApiResponse>;
