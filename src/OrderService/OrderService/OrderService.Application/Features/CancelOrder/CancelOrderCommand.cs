using CommunalService.Domain;
using MediatR;

namespace OrderService.Application.Features.CancelOrder;

public sealed record CancelOrderCommand : IRequest<ApiResponse>
{
    public long Id { get; init; }
    public long CustomerId { get; init; }
    public bool OverrideCustomerScope { get; set; }
    public string Reason { get; init; } = "Customer cancelled";
}
