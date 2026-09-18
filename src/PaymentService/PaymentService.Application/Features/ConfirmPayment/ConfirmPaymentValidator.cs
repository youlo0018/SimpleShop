using FluentValidation;

namespace PaymentService.Application.Features.ConfirmPayment;

/// <summary>
/// 确认支付参数校验：业务单号与库存明细必填，数量非法会在扣减库存前拦截。
/// </summary>
public class ConfirmPaymentValidator : AbstractValidator<ConfirmPaymentCommand>
{
    public ConfirmPaymentValidator()
    {
        RuleFor(x => x.BizNo)
            .NotEmpty().WithMessage("业务单号不能为空")
            .MaximumLength(64).WithMessage("业务单号不能超过64个字符");
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("支付明细不能为空");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.SkuId).GreaterThan(0).WithMessage("SKU 不能为空");
            item.RuleFor(i => i.Quantity).InclusiveBetween(1, 10000).WithMessage("商品数量必须为1-10000");
        });
    }
}
