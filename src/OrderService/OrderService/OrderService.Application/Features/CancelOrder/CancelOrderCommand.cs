using CommunalService.Domain;
using MediatR;

namespace OrderService.Application.Features.CancelOrder;

public sealed record CancelOrderCommand : IRequest<ApiResponse>
{
    /// <summary>主键（雪花 ID）。</summary>
    public long Id { get; init; }
    /// <summary>客户 ID。</summary>
    public long CustomerId { get; init; }
    /// <summary>是否跳过客户归属（网关强制回填，防止越权）。</summary>
    public bool OverrideCustomerScope { get; set; }
    /// <summary>原因/说明。</summary>
    public string Reason { get; init; } = "Customer cancelled";
}
