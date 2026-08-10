using CommunalService.Domain.Enums;
using FluentValidation;
using YukeTools;

namespace CustomerService.Application.Features.Customer.CreateCustomer;

public class CreateCustomerValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("邮箱不能为空")
            .EmailAddress().WithMessage("邮箱格式不正确");

        RuleFor(x => x.pwd)
            .NotEmpty().WithMessage("密码不能为空")
            .MinimumLength(8).WithMessage("密码长度不能少于8位")
            .MaximumLength(32).WithMessage("密码长度不能大于32位");

        RuleFor(x => x.RegisterSource)
            .GreaterThan(0).WithMessage("用户注册来源错误")
            .Must(CusromerSourceCode.CheckSource).WithMessage("用户注册来源错误");
    }
}