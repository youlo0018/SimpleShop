using FluentValidation;

namespace CartService.Application.Features.Cart.Remove;

/// <summary>
/// 移除购物车条目参数校验：UserId 由控制器按登录态强制回填，SKU 必填。
/// </summary>
public class RemoveCartItemValidator : AbstractValidator<RemoveCartItemCommand>
{
    public RemoveCartItemValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0).WithMessage("用户不能为空");
        RuleFor(x => x.SkuId).GreaterThan(0).WithMessage("SKU 不能为空");
    }
}
