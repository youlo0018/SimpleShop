using FluentValidation;

namespace ProductService.Application.Features.Category.UpdateCategory;

public class UpdateCategoryValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("分类标识缺失");
        RuleFor(x => x.Name).NotEmpty().WithMessage("分类名称不能为空");
    }
}
