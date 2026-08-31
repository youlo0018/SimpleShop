using CommunalService.Domain;
using MediatR;

namespace MerchantPlatformService.Application.Features.Platforms.SetEnabled;

public record SetPlatformEnabledCommand(long Id, bool IsEnabled) : IRequest<ApiResponse>;
