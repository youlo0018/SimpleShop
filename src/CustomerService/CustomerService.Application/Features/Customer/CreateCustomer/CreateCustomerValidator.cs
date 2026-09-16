using System.Text.RegularExpressions;
using CommunalService.Domain.Enums;
using FluentValidation;

namespace CustomerService.Application.Features.Customer.CreateCustomer;

/// <summary>
/// 客户注册参数校验：用户名/邮箱/密码必填并限制长度（对齐 customer 表列宽）；手机号可选但填了必须合法。
/// </summary>
public class CreateCustomerValidator : AbstractValidator<CreateCustomerCommand>
{
    private static readonly Regex PhoneRegex = new("^1[3-9]\\d{9}$", RegexOptions.Compiled);

    public CreateCustomerValidator()
    {
        RuleFor(x => x.CustomerName)
            .NotEmpty().WithMessage("用户名不能为空")
            .MaximumLength(10).WithMessage("用户名不能超过10个字符");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("邮箱不能为空")
            .EmailAddress().WithMessage("邮箱格式不正确")
            .MaximumLength(64).WithMessage("邮箱不能超过64个字符");

        RuleFor(x => x.Phone)
            .Must(phone => string.IsNullOrWhiteSpace(phone) || PhoneRegex.IsMatch(phone))
            .WithMessage("手机号格式不正确")
            .MaximumLength(18).WithMessage("手机号不能超过18个字符");

        RuleFor(x => x.pwd)
            .NotEmpty().WithMessage("密码不能为空")
            .MinimumLength(8).WithMessage("密码长度不能少于8位")
            .MaximumLength(32).WithMessage("密码长度不能大于32位");

        RuleFor(x => x.Gender)
            .InclusiveBetween(0, 2).WithMessage("性别值不正确");

        RuleFor(x => x.Birth)
            .Must(birth => birth >= new DateTime(1900, 1, 1) && birth <= DateTime.Today)
            .When(x => x.Birth != default)
            .WithMessage("生日日期不正确");

        RuleFor(x => x.RegisterSource)
            .GreaterThan(0).WithMessage("用户注册来源错误")
            .Must(CusromerSourceCode.CheckSource).WithMessage("用户注册来源错误");
    }
}
