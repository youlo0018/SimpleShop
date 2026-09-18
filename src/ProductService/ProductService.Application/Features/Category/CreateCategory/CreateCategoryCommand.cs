using CommunalService.Domain;
using MediatR;

namespace ProductService.Application.Features.Category.CreateCategory;

/// <summary>新增分类（最多三级）。</summary>
public record CreateCategoryCommand(string Name, long ParentId = 0, int Sort = 0) : IRequest<ApiResponse>;
