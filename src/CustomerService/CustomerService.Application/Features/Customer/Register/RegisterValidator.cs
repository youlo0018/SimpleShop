using System.Text.RegularExpressions;
using CustomerService.Domain.Enums;
using FluentValidation;

namespace CustomerService.Application.Features.Customer.Register;

/// <summary>
/// 注册校验：平台必选、用户名 3-64、密码至少 8 位含字母和数字、手机/邮箱格式、
/// 必须勾选用户协议、注册来源必须为已定义枚举。
/// </summary>
public class RegisterValidator : AbstractValidator<RegisterCommand>
{
    /// <summary>手机号正则（与实体列宽/校验规则对齐）。</summary>
    private static readonly Regex PhoneRegex = new("^1[3-9]\\d{9}$", RegexOptions.Compiled);
    /// <summary>邮箱正则。</summary>
    private static readonly Regex EmailRegex = new("^[^\\s@]+@[^\\s@]+\\.[^\\s@]{2,}$", RegexOptions.Compiled);

    /// <summary>规则覆盖：平台/用户名/密码强度/手机号/邮箱/协议/来源。</summary>
    public RegisterValidator()
    {
        RuleFor(x => x.PlatformId).GreaterThan(0).WithMessage("请先选择平台");
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("用户名不能为空")
            .Length(3, 64).WithMessage("用户名必须为3-64个字符");
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("密码不能为空")
            .MinimumLength(8).WithMessage("密码至少8位且包含字母和数字")
            .Must(password => password.Any(char.IsDigit) && password.Any(char.IsLetter))
            .WithMessage("密码至少8位且包含字母和数字");
        RuleFor(x => x.Phone)
            .Must(phone => string.IsNullOrWhiteSpace(phone) || PhoneRegex.IsMatch(phone))
            .WithMessage("手机号格式不正确");
        RuleFor(x => x.Email)
            .Must(email => string.IsNullOrWhiteSpace(email) || EmailRegex.IsMatch(email))
            .WithMessage("邮箱格式不正确");
        RuleFor(x => x.AgreedAgreement)
            .Equal(true).WithMessage("请阅读并同意用户协议");
        RuleFor(x => x.RegisterSource)
            .Must(source => Enum.IsDefined(typeof(RegisterSource), source))
            .WithMessage("注册来源不正确");
    }
}
