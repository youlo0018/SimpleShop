using FluentValidation;

namespace PaymentService.Application.Features.CreatePayment;

/// <summary>
/// 创建支付单参数校验：金额必须为正且最多两位小数；明细商品与数量必须合法，防止空明细或零负数量进入库存链路。
/// </summary>
public class CreatePaymentValidator : AbstractValidator<CreatePaymentCommand>
{
    public CreatePaymentValidator()
    {
        RuleFor(x => x.BizNo)
            .NotEmpty().WithMessage("业务单号不能为空")
            .MaximumLength(64).WithMessage("业务单号不能超过64个字符");
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("支付金额必须大于0")
            .Must(amount => decimal.Round(amount, 2) == amount).WithMessage("支付金额最多保留两位小数");
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("支付明细不能为空");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.SkuId).GreaterThan(0).WithMessage("SKU 不能为空");
            item.RuleFor(i => i.Quantity).InclusiveBetween(1, 10000).WithMessage("商品数量必须为1-10000");
        });
    }
}
