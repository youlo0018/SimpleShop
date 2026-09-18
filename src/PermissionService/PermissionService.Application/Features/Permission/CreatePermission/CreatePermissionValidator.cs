using FluentValidation;

namespace PermissionService.Application.Features.Permission.CreatePermission;

public class CreatePermissionValidator : AbstractValidator<CreatePermissionCommand>
{
    public CreatePermissionValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("权限编码、名称和接口路径必填");
        RuleFor(x => x.Name).NotEmpty().WithMessage("权限编码、名称和接口路径必填");
        RuleFor(x => x.InterfacePath)
            .NotEmpty().WithMessage("权限编码、名称和接口路径必填")
            .Must(path => path.StartsWith("/gateway/", StringComparison.OrdinalIgnoreCase))
            .WithMessage("接口路径必须以 /gateway/ 开头");
        RuleFor(x => x.AllowedScopes)
            .Must(scopes => scopes is 1 or 2 or 3).WithMessage("权限层面无效");
    }
}
