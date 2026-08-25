using MediatR;

namespace MerchantPlatformService.Application.Features.Platforms.Create;

public sealed record CreatePlatformCommand : IRequest<object>
{
    public string PlatformCode { get; init; } = string.Empty;
    public string PlatformName { get; init; } = string.Empty;
    public string ContactEmail { get; init; } = string.Empty;
    public decimal DefaultCommissionRate { get; init; }
}
