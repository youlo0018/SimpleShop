using FluentValidation;

namespace MerchantPlatformService.Application.Features.Platforms.SetEnabled;

/// <summary>
/// 平台启停参数校验：平台 ID 必填；IsEnabled 为布尔无需额外校验。
/// </summary>
public class SetPlatformEnabledValidator : AbstractValidator<SetPlatformEnabledCommand>
{
    public SetPlatformEnabledValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("平台不能为空");
    }
}
