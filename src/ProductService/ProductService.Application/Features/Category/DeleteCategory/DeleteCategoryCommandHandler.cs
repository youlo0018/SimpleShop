using CommunalService.Domain;
using CommunalService.Domain.Enums;
using MediatR;
using ProductService.Domain.IRepository;

namespace ProductService.Application.Features.Category.DeleteCategory;

/// <summary>
/// 删除分类（软删除）：有子分类或有在挂商品时拒绝，防止商品/分类树悬空。
/// </summary>
public class DeleteCategoryCommandHandler(IProductAdminRepository repository)
    : IRequestHandler<DeleteCategoryCommand, ApiResponse>
{
    public async Task<ApiResponse> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await repository.GetCategoryAsync(request.Id, cancellationToken);
        if (category is null || category.IsDeleted)
            return ApiResults.Fail(BaseApiResponseCode.NotFound, "分类不存在");
        if (await repository.CategoryHasChildrenAsync(request.Id, cancellationToken))
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "请先删除或移走子分类");
        if (await repository.CategoryHasProductsAsync(request.Id, cancellationToken))
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "分类下仍有商品，不能删除");

        return await repository.SoftDeleteCategoryAsync(request.Id, cancellationToken)
            ? ApiResults.Ok(new { success = true })
            : ApiResults.Fail(BaseApiResponseCode.NotFound, "分类不存在");
    }
}
