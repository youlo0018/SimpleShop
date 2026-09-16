using FluentValidation;

namespace ProductService.Application.Features.Category.DisableCategory;

/// <summary>
/// 分类启停参数校验：分类 ID 必填。
/// </summary>
public class DisableCategoryValidator : AbstractValidator<DisableCategoryCommand>
{
    public DisableCategoryValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("分类不能为空");
    }
}
