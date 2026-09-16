using FluentValidation;

namespace PaymentService.Application.Features.Payment.RefundDetail;

/// <summary>
/// 退款详情参数校验：退款单 ID 必填。
/// </summary>
public class RefundDetailValidator : AbstractValidator<RefundDetailQuery>
{
    public RefundDetailValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("退款单不能为空");
    }
}
