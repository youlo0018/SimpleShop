using CommunalService.Domain;
using MediatR;

namespace MerchantPlatformService.Application.Features.Platforms.Edit;

public record EditPlatformCommand(long Id, string PlatformCode, string PlatformName, string ContactEmail,
    decimal DefaultCommissionRate) : IRequest<ApiResponse>;
