using FluentValidation;

namespace ProductService.Application.Features.Product.OffShelfProduct;

/// <summary>
/// 商品下架参数校验：商品 ID 必填。
/// </summary>
public class OffShelfProductValidator : AbstractValidator<OffShelfProductCommand>
{
    public OffShelfProductValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("商品不能为空");
    }
}
