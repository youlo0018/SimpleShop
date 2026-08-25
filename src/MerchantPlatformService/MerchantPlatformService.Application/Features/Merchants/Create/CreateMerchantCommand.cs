using MediatR;

namespace MerchantPlatformService.Application.Features.Merchants.Create;

public sealed record CreateMerchantCommand : IRequest<object>
{
    public long PlatformId { get; init; }
    public string MerchantName { get; init; } = string.Empty;
    public string ContactName { get; init; } = string.Empty;
    public string ContactPhone { get; init; } = string.Empty;
    public string ContactEmail { get; init; } = string.Empty;
    public decimal CommissionRate { get; init; }
}
