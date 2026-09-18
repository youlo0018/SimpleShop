using FluentValidation;

namespace MerchantPlatformService.Application.Features.Merchants.Create;

public sealed class CreateMerchantValidator : AbstractValidator<CreateMerchantCommand>
{
    public CreateMerchantValidator()
    {
        RuleFor(x => x.PlatformId).GreaterThan(0).WithMessage("必须选择有效平台");
        RuleFor(x => x.MerchantName).NotEmpty().MaximumLength(64);
        RuleFor(x => x.ContactName).NotEmpty().MaximumLength(32);
        RuleFor(x => x.ContactPhone).NotEmpty().Matches(@"^1[3-9]\d{9}$").WithMessage("手机号格式不正确");
        RuleFor(x => x.ContactEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.CommissionRate).InclusiveBetween(0, 100);
    }
}
