using FluentValidation;

namespace MerchantPlatformService.Application.Features.Platforms.Create;

/// <summary>平台创建校验：编码必须为 6 个字母（创建时由用户输入，全局唯一由 Handler 兜底）。</summary>
public sealed class CreatePlatformValidator : AbstractValidator<CreatePlatformCommand>
{
    /// <summary>规则覆盖：6 位字母编码、名称、邮箱、费率区间。</summary>
    public CreatePlatformValidator()
    {
        RuleFor(x => x.PlatformCode)
            .NotEmpty().WithMessage("请输入平台编码")
            .Matches("^[a-zA-Z]{6}$").WithMessage("平台编码必须为6个字母");
        RuleFor(x => x.PlatformName).NotEmpty().WithMessage("请输入平台名称").MaximumLength(64);
        RuleFor(x => x.ContactEmail).NotEmpty().WithMessage("请输入联系邮箱").EmailAddress().WithMessage("邮箱格式不正确");
        RuleFor(x => x.DefaultCommissionRate).InclusiveBetween(0, 100).WithMessage("佣金比例必须在0-100之间");
    }
}
