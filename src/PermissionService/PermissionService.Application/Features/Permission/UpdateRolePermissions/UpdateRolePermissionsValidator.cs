using FluentValidation;

namespace PermissionService.Application.Features.Permission.UpdateRolePermissions;

/// <summary>
/// 编辑角色参数校验：角色 ID 与名称必填，说明/权限编码长度对齐实体列宽（权限集合允许为空，表示收回全部权限）。
/// </summary>
public class UpdateRolePermissionsValidator : AbstractValidator<UpdateRolePermissionsCommand>
{
    public UpdateRolePermissionsValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("角色不能为空");
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("角色名称不能为空")
            .MaximumLength(64).WithMessage("角色名称不能超过64个字符");
        RuleFor(x => x.Description).MaximumLength(255).WithMessage("角色说明不能超过255个字符");
        RuleForEach(x => x.Permissions)
            .NotEmpty().WithMessage("权限编码不能为空")
            .MaximumLength(80).WithMessage("权限编码不能超过80个字符");
    }
}
