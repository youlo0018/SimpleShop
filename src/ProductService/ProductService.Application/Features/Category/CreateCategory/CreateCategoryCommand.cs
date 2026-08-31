using CommunalService.Domain;
using MediatR;

namespace ProductService.Application.Features.Category.CreateCategory;

public record CreateCategoryCommand(string Name, long ParentId = 0, int Sort = 0) : IRequest<ApiResponse>;
