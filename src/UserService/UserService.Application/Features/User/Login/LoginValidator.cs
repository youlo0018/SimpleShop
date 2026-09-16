using FluentValidation;

namespace UserService.Application.Features.User.Login;

/// <summary>
/// 登录参数校验：只做非空与长度上限（避免超长输入打库）；不校验最小长度，兼容历史账号。
/// </summary>
public class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("请输入用户名")
            .MaximumLength(64).WithMessage("用户名不能超过64个字符");
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("请输入密码")
            .MaximumLength(128).WithMessage("密码长度不正确");
    }
}
