using FluentValidation;

namespace ProductService.Application.Features.Product.CreateProduct;

/// <summary>
/// 创建商品参数校验：名称/主图/分类必填，SKU 编码唯一、售价与库存合法、原价不得低于售价（长度对齐实体列）。
/// </summary>
public class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("商品名称必填").MaximumLength(40).WithMessage("商品名称过长");
        RuleFor(x => x.MainImage).NotEmpty().WithMessage("商品主图必填").MaximumLength(255).WithMessage("商品主图地址过长");
        RuleFor(x => x.Description).MaximumLength(255).WithMessage("商品描述不能超过255个字符");
        RuleFor(x => x.CategoryId).GreaterThan(0).WithMessage("分类必选");
        RuleFor(x => x.Skus).NotEmpty().WithMessage("至少配置一个SKU");
        RuleForEach(x => x.Skus).ChildRules(sku =>
        {
            sku.RuleFor(s => s.SkuCode).NotEmpty().WithMessage("SKU编码必填").MaximumLength(40).WithMessage("SKU编码过长");
            sku.RuleFor(s => s.Price).GreaterThan(0).WithMessage("SKU售价必须大于0");
            sku.RuleFor(s => s.OriginalPrice)
                .GreaterThanOrEqualTo(0).WithMessage("SKU原价不能为负数")
                .Must((item, originalPrice) => originalPrice <= 0 || originalPrice >= item.Price)
                .WithMessage("SKU原价不能低于售价");
            sku.RuleFor(s => s.Stock).GreaterThan(0).WithMessage("SKU库存必须大于0");
            sku.RuleFor(s => s.Image).MaximumLength(255).WithMessage("SKU图片地址过长");
            sku.RuleFor(s => s.SpecName).MaximumLength(40).WithMessage("规格名不能超过40个字符");
            sku.RuleFor(s => s.SpecValue).MaximumLength(120).WithMessage("规格值不能超过120个字符");
        });
        RuleFor(x => x.Skus)
            .Must(skus => skus.GroupBy(sku => sku.SkuCode.Trim(), StringComparer.OrdinalIgnoreCase).All(group => group.Count() == 1))
            .WithMessage("SKU编码不能重复");
    }
}
