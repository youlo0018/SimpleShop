using CommunalService.Domain;
using MediatR;

namespace ProductService.Application.Features.Category.DeleteCategory;

/// <summary>删除分类（有子级或商品时禁止）。</summary>
/// <param name="Id">主键。</param>
public record DeleteCategoryCommand(long Id) : IRequest<ApiResponse>;
