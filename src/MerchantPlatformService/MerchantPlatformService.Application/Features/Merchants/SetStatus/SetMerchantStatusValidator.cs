using FluentValidation;
using MerchantPlatformService.Domain.Enums;

namespace MerchantPlatformService.Application.Features.Merchants.SetStatus;

/// <summary>
/// 商户状态变更参数校验：ID 必填；Status 必须是 MerchantStatus 已定义值（0/10/20/30/40）。
/// </summary>
public class SetMerchantStatusValidator : AbstractValidator<SetMerchantStatusCommand>
{
    public SetMerchantStatusValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("商户不能为空");
        RuleFor(x => x.Status)
            .Must(status => Enum.IsDefined(typeof(MerchantStatus), status))
            .WithMessage("商户状态值不正确");
    }
}
