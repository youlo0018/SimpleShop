using FluentValidation;

namespace ProductService.Application.Features.Product.CreateProduct;

public class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("商品名称必填").MaximumLength(40).WithMessage("商品名称过长");
        RuleFor(x => x.MainImage).NotEmpty().WithMessage("商品主图必填");
        RuleFor(x => x.CategoryId).GreaterThan(0).WithMessage("分类必选");
        RuleFor(x => x.Skus).NotEmpty().WithMessage("至少配置一个SKU");
        RuleForEach(x => x.Skus).ChildRules(sku =>
        {
            sku.RuleFor(s => s.SkuCode).NotEmpty().WithMessage("SKU编码必填").MaximumLength(40).WithMessage("SKU编码过长");
            sku.RuleFor(s => s.Price).GreaterThan(0).WithMessage("SKU售价必须大于0");
            sku.RuleFor(s => s.Stock).GreaterThan(0).WithMessage("SKU库存必须大于0");
        });
        RuleFor(x => x.Skus)
            .Must(skus => skus.GroupBy(sku => sku.SkuCode.Trim(), StringComparer.OrdinalIgnoreCase).All(group => group.Count() == 1))
            .WithMessage("SKU编码不能重复");
    }
}
