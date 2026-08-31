using CommunalService.Domain;
using CommunalService.Domain.Enums;
using MediatR;
using EntityCategory = ProductService.Domain.Entity.Category;
using ProductService.Domain.IRepository;

namespace ProductService.Application.Features.Category.CreateCategory;

/// <summary>
/// 创建分类：沿父链向上走最多 2 层（第 3 层循环即会创建第 4 级）——强制分类不超过 3 级。
/// </summary>
public class CreateCategoryCommandHandler(IProductAdminRepository repository)
    : IRequestHandler<CreateCategoryCommand, ApiResponse>
{
    public async Task<ApiResponse> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        long parentId = request.ParentId;
        int depth = 0;
        while (parentId > 0)
        {
            depth++;
            var parent = await repository.GetCategoryAsync(parentId, cancellationToken);
            if (parent is null || parent.IsDeleted)
                return ApiResults.Fail(BaseApiResponseCode.BadRequest, "父级分类不存在");
            // 两层父级允许第三级；第三次循环说明会创建第四级。
            if (depth > 2)
                return ApiResults.Fail(BaseApiResponseCode.BadRequest, "分类不能超过3级");
            parentId = parent.ParentId;
        }

        var category = new EntityCategory { Name = request.Name, ParentId = request.ParentId, Sort = request.Sort };
        await repository.InsertCategoryAsync(category, cancellationToken);
        return ApiResults.Ok(category);
    }
}
