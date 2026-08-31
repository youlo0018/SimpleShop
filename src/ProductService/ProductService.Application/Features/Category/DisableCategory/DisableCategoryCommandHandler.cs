using CommunalService.Domain;
using CommunalService.Domain.Enums;
using MediatR;
using ProductService.Domain.IRepository;

namespace ProductService.Application.Features.Category.DisableCategory;

/// <summary>
/// 启用/停用分类：停用后前台分类树不可见，且不能作为新商品的归属分类。
/// </summary>
public class DisableCategoryCommandHandler(IProductAdminRepository repository)
    : IRequestHandler<DisableCategoryCommand, ApiResponse>
{
    public async Task<ApiResponse> Handle(DisableCategoryCommand request, CancellationToken cancellationToken)
    {
        return await repository.SetCategoryActiveAsync(request.Id, request.IsActive, cancellationToken)
            ? ApiResults.Ok(new { success = true })
            : ApiResults.Fail(BaseApiResponseCode.NotFound, "分类不存在");
    }
}
