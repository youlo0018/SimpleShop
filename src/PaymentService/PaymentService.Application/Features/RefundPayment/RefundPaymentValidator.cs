using FluentValidation;

namespace PaymentService.Application.Features.RefundPayment;

/// <summary>
/// 申请退款参数校验：金额为正且最多两位小数、原因必填（255 上限对齐 refund_order.reason 列）。
/// </summary>
public class RefundPaymentValidator : AbstractValidator<RefundPaymentCommand>
{
    public RefundPaymentValidator()
    {
        RuleFor(x => x.BizNo)
            .NotEmpty().WithMessage("业务单号不能为空")
            .MaximumLength(64).WithMessage("业务单号不能超过64个字符");
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("退款金额必须大于0")
            .Must(amount => decimal.Round(amount, 2) == amount).WithMessage("退款金额最多保留两位小数");
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("退款原因不能为空")
            .MaximumLength(255).WithMessage("退款原因不能超过255个字符");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.SkuId).GreaterThan(0).WithMessage("SKU 不能为空");
            item.RuleFor(i => i.Quantity).InclusiveBetween(1, 10000).WithMessage("回补数量必须为1-10000");
        });
    }
}
