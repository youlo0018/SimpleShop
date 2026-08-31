using CommunalService.Domain;
using MediatR;

namespace ProductService.Application.Features.Category.DisableCategory;

public record DisableCategoryCommand(long Id, bool IsActive) : IRequest<ApiResponse>;
