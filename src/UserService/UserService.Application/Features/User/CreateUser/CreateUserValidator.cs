using System.Text.RegularExpressions;
using FluentValidation;

namespace UserService.Application.Features.User.CreateUser;

public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    /// <summary>手机号正则（与实体列宽/校验规则对齐）。</summary>
    private static readonly Regex PhoneRegex = new("^1[3-9]\\d{9}$", RegexOptions.Compiled);
    /// <summary>邮箱正则。</summary>
    private static readonly Regex EmailRegex = new("^[^\\s@]+@[^\\s@]+\\.[^\\s@]{2,}$", RegexOptions.Compiled);

    public CreateUserValidator()
    {
        // 账号域分离：UserService 只管理后台账号，客户账号必须走 CustomerService 注册。
        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("请选择角色")
            .Must(role => !string.Equals(role, "customer", StringComparison.OrdinalIgnoreCase))
            .WithMessage("客户账号请在商城注册，后台仅管理平台/商户账号");
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
    }
}
