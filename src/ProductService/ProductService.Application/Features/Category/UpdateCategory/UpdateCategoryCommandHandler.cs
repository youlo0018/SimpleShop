using CommunalService.Domain;
using CommunalService.Domain.Enums;
using MediatR;
using ProductService.Domain.IRepository;

namespace ProductService.Application.Features.Category.UpdateCategory;

/// <summary>
/// 更新分类：禁止把父级设为自己的子孙（成环）；同样强制 3 级上限。
/// </summary>
public class UpdateCategoryCommandHandler(IProductAdminRepository repository)
    : IRequestHandler<UpdateCategoryCommand, ApiResponse>
{
    public async Task<ApiResponse> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await repository.GetCategoryAsync(request.Id, cancellationToken);
        if (category is null || category.IsDeleted)
            return ApiResults.Fail(BaseApiResponseCode.NotFound, "分类不存在");
        if (request.ParentId > 0 && await repository.IsDescendantAsync(request.Id, request.ParentId, cancellationToken))
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "父级分类不能是当前分类的子孙分类");

        long checkedParentId = request.ParentId;
        int depth = 0;
        while (checkedParentId > 0)
        {
            depth++;
            var parent = await repository.GetCategoryAsync(checkedParentId, cancellationToken);
            if (parent is null || parent.IsDeleted || parent.Id == request.Id)
                return ApiResults.Fail(BaseApiResponseCode.BadRequest, "父级分类无效或形成循环");
            if (depth > 2) return ApiResults.Fail(BaseApiResponseCode.BadRequest, "分类不能超过3级");
            checkedParentId = parent.ParentId;
        }

        category.Name = request.Name;
        category.ParentId = request.ParentId;
        category.Sort = request.Sort;
        await repository.UpdateCategoryAsync(category, cancellationToken);
        return ApiResults.Ok(new { success = true });
    }
}
