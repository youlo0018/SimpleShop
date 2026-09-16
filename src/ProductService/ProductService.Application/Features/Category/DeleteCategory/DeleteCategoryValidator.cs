using FluentValidation;

namespace ProductService.Application.Features.Category.DeleteCategory;

/// <summary>
/// 删除分类参数校验：分类 ID 必填。
/// </summary>
public class DeleteCategoryValidator : AbstractValidator<DeleteCategoryCommand>
{
    public DeleteCategoryValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("分类不能为空");
    }
}
