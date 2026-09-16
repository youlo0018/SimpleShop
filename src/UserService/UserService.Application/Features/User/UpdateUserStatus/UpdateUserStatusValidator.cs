using FluentValidation;

namespace UserService.Application.Features.User.UpdateUserStatus;

/// <summary>
/// 启用/禁用账号参数校验：用户 ID 必填；IsEnabled 为布尔无需额外校验。
/// </summary>
public class UpdateUserStatusValidator : AbstractValidator<UpdateUserStatusCommand>
{
    public UpdateUserStatusValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("用户不能为空");
    }
}
