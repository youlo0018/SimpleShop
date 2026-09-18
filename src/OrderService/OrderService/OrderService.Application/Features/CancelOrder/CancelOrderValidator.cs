using FluentValidation;

namespace OrderService.Application.Features.CancelOrder;

/// <summary>
/// 取消订单参数校验：订单 ID 必填；原因可选（空时 Handler 用默认文案），不超过 order.cancel_reason 的 255 字符。
/// </summary>
public class CancelOrderValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("订单不能为空");
        RuleFor(x => x.Reason).MaximumLength(255).WithMessage("取消原因不能超过255个字符");
    }
}
