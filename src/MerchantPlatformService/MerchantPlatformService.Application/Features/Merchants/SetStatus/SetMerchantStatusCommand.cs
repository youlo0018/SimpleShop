using CommunalService.Domain;
using MediatR;

namespace MerchantPlatformService.Application.Features.Merchants.SetStatus;

public record SetMerchantStatusCommand(long Id, int Status) : IRequest<ApiResponse>;
