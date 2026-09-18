using FluentValidation;

namespace CartService.Application.Features.Cart.Get;

/// <summary>
/// 购物车查询参数校验：UserId 由控制器按登录态强制回填，这里防御非法调用。
/// </summary>
public class GetCartValidator : AbstractValidator<GetCartQuery>
{
    public GetCartValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0).WithMessage("用户不能为空");
    }
}
