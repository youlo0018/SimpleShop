using FluentValidation;

namespace MerchantPlatformService.Application.Features.Merchants.Review;

/// <summary>商户审核参数校验：商户必填、必须给出审核结论、驳回原因不超过255字符。</summary>
public class ReviewMerchantValidator : AbstractValidator<ReviewMerchantCommand>
{
    /// <summary>规则覆盖：商户 ID、审核结论必填、原因长度。</summary>
    public ReviewMerchantValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("商户不能为空");
        RuleFor(x => x)
            .Must(command => command.Approved.HasValue || command.Status.HasValue)
            .WithMessage("请提交审核结论");
        RuleFor(x => x.Reason).MaximumLength(255).WithMessage("审核原因不能超过255个字符");
    }
}
