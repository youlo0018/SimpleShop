using FluentValidation;

namespace MerchantPlatformService.Application.Features.Merchants.Review;

/// <summary>
/// 商户审核参数校验：商户 ID 必填；审核结论由控制器从 Approved/Status 归一化，拒绝原因不超过255字符。
/// </summary>
public class ReviewMerchantValidator : AbstractValidator<ReviewMerchantCommand>
{
    public ReviewMerchantValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("商户不能为空");
        RuleFor(x => x.Reason).MaximumLength(255).WithMessage("审核原因不能超过255个字符");
    }
}
