using FluentValidation;

namespace OrderService.Application.Features.GetOrder;

/// <summary>
/// 订单查询参数校验：订单 ID 必填；CustomerId 由控制器按登录态回填，不在校验范围。
/// </summary>
public class GetOrderValidator : AbstractValidator<GetOrderQuery>
{
    public GetOrderValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("订单不能为空");
    }
}
