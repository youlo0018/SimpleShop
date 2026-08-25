using MediatR;

namespace ProductService.Application.Features.Category.GetCategoryTree;

public record GetCategoryTreeCommand : IRequest<object>
{
}
