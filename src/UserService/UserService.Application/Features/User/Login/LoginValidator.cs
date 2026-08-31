using FluentValidation;

namespace UserService.Application.Features.User.Login;

public class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().WithMessage("请输入用户名");
        RuleFor(x => x.Password).NotEmpty().WithMessage("请输入密码");
    }
}
