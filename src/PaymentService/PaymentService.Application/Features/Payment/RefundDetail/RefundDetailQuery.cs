using CommunalService.Domain;
using MediatR;

namespace PaymentService.Application.Features.Payment.RefundDetail;

/// <summary>退款单详情查询（后台/用户按租户裁剪）。</summary>
/// <param name="Id">主键。</param>
public record RefundDetailQuery(long Id) : IRequest<ApiResponse>;
