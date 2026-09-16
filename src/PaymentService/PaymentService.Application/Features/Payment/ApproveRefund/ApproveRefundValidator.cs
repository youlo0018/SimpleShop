using FluentValidation;

namespace PaymentService.Application.Features.Payment.ApproveRefund;

/// <summary>
/// 同意退款参数校验：退款单 ID 必填；审批备注可选但不超过255字符。
/// </summary>
public class ApproveRefundValidator : AbstractValidator<ApproveRefundCommand>
{
    public ApproveRefundValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("退款单不能为空");
        RuleFor(x => x.Reason).MaximumLength(255).WithMessage("审批备注不能超过255个字符");
    }
}
