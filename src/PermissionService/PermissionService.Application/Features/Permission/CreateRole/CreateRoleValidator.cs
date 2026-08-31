using FluentValidation;

namespace PermissionService.Application.Features.Permission.CreateRole;

public class CreateRoleValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("角色编码和名称不能为空");
        RuleFor(x => x.Name).NotEmpty().WithMessage("角色编码和名称不能为空");
        RuleFor(x => x.TenantType).Must(type => type is 1 or 2).WithMessage("角色范围无效");
    }
}
