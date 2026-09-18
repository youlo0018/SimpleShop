using System.Text.RegularExpressions;
using FluentValidation;

namespace UserService.Application.Features.User.UpdateUser;

public class UpdateUserValidator : AbstractValidator<UpdateUserCommand>
{
    /// <summary>手机号正则（与实体列宽/校验规则对齐）。</summary>
    private static readonly Regex PhoneRegex = new("^1[3-9]\\d{9}$", RegexOptions.Compiled);
    /// <summary>邮箱正则。</summary>
    private static readonly Regex EmailRegex = new("^[^\\s@]+@[^\\s@]+\\.[^\\s@]{2,}$", RegexOptions.Compiled);

    public UpdateUserValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("用户名不能为空")
            .Length(3, 64).WithMessage("用户名必须为3-64个字符");
        RuleFor(x => x.Password)
            .Must(password => string.IsNullOrEmpty(password) ||
                (password.Length >= 8 && password.Any(char.IsDigit) && password.Any(char.IsLetter)))
            .WithMessage("密码至少8位且包含字母和数字");
        RuleFor(x => x.Phone)
            .Must(phone => string.IsNullOrWhiteSpace(phone) || PhoneRegex.IsMatch(phone))
            .WithMessage("手机号格式不正确");
        RuleFor(x => x.Email)
            .Must(email => string.IsNullOrWhiteSpace(email) || EmailRegex.IsMatch(email))
            .WithMessage("邮箱格式不正确");
        RuleFor(x => x.Avatar).MaximumLength(255).WithMessage("头像地址不能超过255个字符");
        RuleFor(x => x.Gender).InclusiveBetween(0, 2).WithMessage("性别不正确");
        RuleFor(x => x.Birth)
            .Must(birth => birth is null || birth <= DateTime.Today)
            .WithMessage("生日不能晚于今天");
    }
}
