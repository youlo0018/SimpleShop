using FluentValidation;

namespace ProductService.Application.Features.Product.SaveProduct;

public class SaveProductValidator : AbstractValidator<SaveProductCommand>
{
    public SaveProductValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("商品标识缺失");
        RuleFor(x => x.Name).NotEmpty().WithMessage("商品名称必填").MaximumLength(40).WithMessage("商品名称过长");
        RuleFor(x => x.Skus).NotEmpty().WithMessage("至少配置一个SKU");
    }
}
