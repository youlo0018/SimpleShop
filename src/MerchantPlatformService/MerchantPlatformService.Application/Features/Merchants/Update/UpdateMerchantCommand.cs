using CommunalService.Domain;
using MediatR;

namespace MerchantPlatformService.Application.Features.Merchants.Update;

public record UpdateMerchantCommand(long Id, long PlatformId, string MerchantName, string ContactName,
    string ContactPhone, string ContactEmail, decimal CommissionRate) : IRequest<ApiResponse>;
