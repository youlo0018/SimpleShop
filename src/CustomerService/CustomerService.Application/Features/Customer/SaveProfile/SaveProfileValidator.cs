using System.Text.RegularExpressions;
using FluentValidation;

namespace CustomerService.Application.Features.Customer.SaveProfile;

/// <summary>资料编辑校验：头像/邮箱/手机号格式与长度，生日不得晚于今天。</summary>
public class SaveProfileValidator : AbstractValidator<SaveProfileCommand>
{
    /// <summary>手机号正则（与实体列宽/校验规则对齐）。</summary>
    private static readonly Regex PhoneRegex = new("^1[3-9]\\d{9}$", RegexOptions.Compiled);
    /// <summary>邮箱正则。</summary>
    private static readonly Regex EmailRegex = new("^[^\\s@]+@[^\\s@]+\\.[^\\s@]{2,}$", RegexOptions.Compiled);

    /// <summary>规则覆盖：头像长度、性别枚举、生日范围、邮箱/手机号格式。</summary>
    public SaveProfileValidator()
    {
        RuleFor(x => x.Avatar).MaximumLength(255).WithMessage("头像地址不能超过255个字符");
        RuleFor(x => x.Gender).InclusiveBetween(0, 2).WithMessage("性别不正确");
        RuleFor(x => x.Birth)
            .Must(birth => birth is null || birth <= DateTime.Today)
            .WithMessage("生日不能晚于今天");
        RuleFor(x => x.Email)
            .Must(email => string.IsNullOrWhiteSpace(email) || EmailRegex.IsMatch(email))
            .WithMessage("邮箱格式不正确");
        RuleFor(x => x.Phone)
            .Must(phone => string.IsNullOrWhiteSpace(phone) || PhoneRegex.IsMatch(phone))
            .WithMessage("手机号格式不正确");
    }
}
