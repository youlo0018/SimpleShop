using FluentValidation;

namespace ProductService.Application.Features.Category.CreateCategory;

public class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("分类名称不能为空");
    }
}
