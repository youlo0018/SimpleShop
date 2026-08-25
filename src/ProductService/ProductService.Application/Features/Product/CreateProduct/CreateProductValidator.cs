using FluentValidation;

namespace ProductService.Application.Features.Product.CreateProduct;

public class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(40);
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.Skus).NotEmpty().WithMessage("至少需要一个SKU");
        RuleForEach(x => x.Skus).ChildRules(sku =>
        {
            sku.RuleFor(s => s.SkuCode).NotEmpty().MaximumLength(40);
            sku.RuleFor(s => s.Price).GreaterThan(0);
            sku.RuleFor(s => s.Stock).GreaterThanOrEqualTo(0);
        });
    }
}
