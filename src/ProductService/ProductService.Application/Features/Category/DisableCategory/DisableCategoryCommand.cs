using CommunalService.Domain;
using MediatR;

namespace ProductService.Application.Features.Category.DisableCategory;

/// <summary>启用/停用分类。</summary>
public record DisableCategoryCommand(long Id, bool IsActive) : IRequest<ApiResponse>;
