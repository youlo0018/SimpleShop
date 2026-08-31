using CommunalService.Domain;
using MediatR;

namespace ProductService.Application.Features.Category.UpdateCategory;

public record UpdateCategoryCommand(long Id, string Name, long ParentId = 0, int Sort = 0) : IRequest<ApiResponse>;
