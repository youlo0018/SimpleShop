using FluentValidation;

namespace ProductService.Application.Features.Product.SaveProduct;

/// <summary>
/// 商品编辑参数校验：与创建商品同标准；此前不校验 SKU 导致非法 SKU 被仓储静默跳过，现在直接返回字段错误。
/// </summary>
public class SaveProductValidator : AbstractValidator<SaveProductCommand>
{
    public SaveProductValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("商品标识缺失");
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
