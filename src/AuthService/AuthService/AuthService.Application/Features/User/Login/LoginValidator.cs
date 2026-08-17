using CommunalService.Domain.Enums;
using FluentValidation;

namespace AuthService.Application.Features.User.Login;

public class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("用户名不能为空")
            .MaximumLength(32).WithMessage("用户名或密码错误");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("密码不能为空")
            .MaximumLength(32).WithMessage("用户名或密码错误");

   
    }
}