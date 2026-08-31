using System.Text.RegularExpressions;
using FluentValidation;

namespace MerchantPlatformService.Application.Features.Platforms.Edit;

public class EditPlatformValidator : AbstractValidator<EditPlatformCommand>
{
    public EditPlatformValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("平台标识缺失");
        RuleFor(x => x.PlatformCode)
            .Matches("^[a-zA-Z][a-zA-Z0-9_-]{2,31}$").WithMessage("平台编码格式不正确");
        RuleFor(x => x.PlatformName)
            .NotEmpty().WithMessage("平台名称必须为2-64个字符")
            .MaximumLength(64).WithMessage("平台名称必须为2-64个字符");
        RuleFor(x => x.ContactEmail)
            .Matches("^[^\\s@]+@[^\\s@]+\\.[^\\s@]{2,}$").WithMessage("邮箱格式不正确");
        RuleFor(x => x.DefaultCommissionRate)
            .InclusiveBetween(0, 100).WithMessage("佣金率必须在0-100之间");
    }
}
