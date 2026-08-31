using CommunalService.Domain;
using CommunalService.Domain.Enums;
using MediatR;
using PermissionService.Domain.IRepository;

namespace PermissionService.Application.Features.Permission.UpdateRolePermissions;

/// <summary>
/// 编辑角色名称/描述并整体重写权限勾选；角色编码与租户范围创建后不可变。
/// </summary>
public class UpdateRolePermissionsCommandHandler(IPermissionCenterRepository repository)
    : IRequestHandler<UpdateRolePermissionsCommand, ApiResponse>
{
    public async Task<ApiResponse> Handle(UpdateRolePermissionsCommand request, CancellationToken cancellationToken)
    {
        var role = await repository.GetRoleAsync(request.Id, cancellationToken);
        if (role is null) return ApiResults.Fail(BaseApiResponseCode.NotFound, "角色不存在");

        role.Name = request.Name;
        role.Description = request.Description ?? string.Empty;
        await repository.UpdateRoleAsync(role, cancellationToken);
        await repository.ReplaceRolePermissionsAsync(request.Id, request.Permissions ?? [], cancellationToken);
        return ApiResults.Ok(new { success = true });
    }
}
