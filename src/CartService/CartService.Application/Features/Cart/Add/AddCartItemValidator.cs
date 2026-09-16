using FluentValidation;

namespace CartService.Application.Features.Cart.Add;

/// <summary>
/// 加入购物车参数校验：价格与数量由客户端提交（Cart 服务不查商品库），必须在此兜底，避免负价或超量条目进入下单链路。
/// </summary>
public class AddCartItemValidator : AbstractValidator<AddCartItemCommand>
{
    public AddCartItemValidator()
    {
        RuleFor(x => x.SkuId).GreaterThan(0).WithMessage("SKU 不能为空");
        RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("商品不能为空");
        RuleFor(x => x.ProductName)
            .NotEmpty().WithMessage("商品名称不能为空")
            .MaximumLength(128).WithMessage("商品名称不能超过128个字符");
        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("商品价格必须大于0")
            .Must(price => decimal.Round(price, 2) == price).WithMessage("商品价格最多保留两位小数");
        RuleFor(x => x.Quantity)
            .InclusiveBetween(1, 99).WithMessage("数量必须为1-99");
    }
}
