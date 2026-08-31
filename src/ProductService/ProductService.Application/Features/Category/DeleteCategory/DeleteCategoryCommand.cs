using CommunalService.Domain;
using MediatR;

namespace ProductService.Application.Features.Category.DeleteCategory;

public record DeleteCategoryCommand(long Id) : IRequest<ApiResponse>;
