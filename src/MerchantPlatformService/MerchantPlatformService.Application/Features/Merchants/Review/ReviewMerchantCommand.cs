using MediatR;

namespace MerchantPlatformService.Application.Features.Merchants.Review;

public sealed record ReviewMerchantCommand : IRequest<object>
{
    public long Id { get; init; }
    public bool Approved { get; init; }
    public string? Reason { get; init; }
}
