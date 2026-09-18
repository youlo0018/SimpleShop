using CommunalService.Domain;
using MediatR;

namespace ProductService.Application.Features.Category.UpdateCategory;

/// <summary>更新分类（层级/名称校验在 Handler）。</summary>
public record UpdateCategoryCommand(long Id, string Name, long ParentId = 0, int Sort = 0) : IRequest<ApiResponse>;
