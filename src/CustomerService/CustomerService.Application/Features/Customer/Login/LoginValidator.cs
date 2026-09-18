using FluentValidation;

namespace CustomerService.Application.Features.Customer.Login;

/// <summary>登录参数校验：平台必选、非空与长度上限（不校验最小长度，兼容历史账号）。</summary>
public class LoginValidator : AbstractValidator<LoginCommand>
{
    /// <summary>规则覆盖：平台/用户名/密码。</summary>
    public LoginValidator()
    {
        RuleFor(x => x.PlatformId).GreaterThan(0).WithMessage("请先选择平台");
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("请输入用户名")
            .MaximumLength(64).WithMessage("用户名不能超过64个字符");
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("请输入密码")
            .MaximumLength(128).WithMessage("密码长度不正确");
    }
}
