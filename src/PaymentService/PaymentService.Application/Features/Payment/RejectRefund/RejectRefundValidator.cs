using FluentValidation;

namespace PaymentService.Application.Features.Payment.RejectRefund;

/// <summary>
/// 拒绝退款参数校验：退款单 ID 必填；拒绝原因可选（空时 Handler 兜底"审核不通过"），不超过255字符。
/// </summary>
public class RejectRefundValidator : AbstractValidator<RejectRefundCommand>
{
    public RejectRefundValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("退款单不能为空");
        RuleFor(x => x.Reason).MaximumLength(255).WithMessage("拒绝原因不能超过255个字符");
    }
}
