using FluentValidation;

namespace PermissionService.Application.Features.Permission.BindUser;

public class BindUserValidator : AbstractValidator<BindUserCommand>
{
    public BindUserValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0).WithMessage("用户和角色必选");
        RuleFor(x => x.RoleId).GreaterThan(0).WithMessage("用户和角色必选");
    }
}
