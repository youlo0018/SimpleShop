using CommunalService.Domain;
using MediatR;

namespace MerchantPlatformService.Application.Features.Platforms.List;

public record ListPlatformsQuery(string Keyword = "", int Page = 1, int PageSize = 10) : IRequest<ApiResponse>;
