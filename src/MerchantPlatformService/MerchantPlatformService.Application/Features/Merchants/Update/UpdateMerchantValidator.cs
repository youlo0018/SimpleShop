using System.Text.RegularExpressions;
using FluentValidation;

namespace MerchantPlatformService.Application.Features.Merchants.Update;

public class UpdateMerchantValidator : AbstractValidator<UpdateMerchantCommand>
{
    public UpdateMerchantValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("商户标识缺失");
        RuleFor(x => x.PlatformId).GreaterThan(0).WithMessage("必须选择有效平台");
        RuleFor(x => x.MerchantName)
            .NotEmpty().WithMessage("商户名称必须为2-64个字符")
            .MaximumLength(64).WithMessage("商户名称必须为2-64个字符");
        RuleFor(x => x.ContactName)
            .NotEmpty().WithMessage("联系人不能超过32个字符")
            .MaximumLength(32).WithMessage("联系人不能超过32个字符");
        RuleFor(x => x.ContactPhone)
            .Matches("^1[3-9]\\d{9}$").WithMessage("手机号格式不正确");
        RuleFor(x => x.ContactEmail)
            .Matches("^[^\\s@]+@[^\\s@]+\\.[^\\s@]{2,}$").WithMessage("邮箱格式不正确");
        RuleFor(x => x.CommissionRate)
            .InclusiveBetween(0, 100).WithMessage("佣金率必须在0-100之间");
    }
}
