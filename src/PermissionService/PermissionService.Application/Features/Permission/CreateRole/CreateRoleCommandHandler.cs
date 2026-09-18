using CommunalService.Domain;
using CommunalService.Domain.Enums;
using MediatR;
using PermissionService.Domain.Entity;
using PermissionService.Domain.IRepository;

namespace PermissionService.Application.Features.Permission.CreateRole;

/// <summary>
/// 创建角色：编码查重 → 落库 → ReplaceRolePermissions 物理重写角色-权限映射（唯一约束决定不能软删）。
/// </summary>
public class CreateRoleCommandHandler(IPermissionCenterRepository repository)
    : IRequestHandler<CreateRoleCommand, ApiResponse>
{
    /// <summary>处理入口：创建角色：编码查重 → 落库 → ReplaceRolePermissions 物理重写角色-权限映射（唯一约束决定不能软删）。</summary>
    public async Task<ApiResponse> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        if (await repository.RoleCodeExistsAsync(request.Code, cancellationToken))
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "角色编码已存在");

        var role = new Role
        {
            Code = request.Code.Trim(), Name = request.Name.Trim(), TenantType = request.TenantType,
            Description = request.Description ?? string.Empty
        };
        await repository.InsertRoleAsync(role, cancellationToken);
        await repository.ReplaceRolePermissionsAsync(role.Id, request.Permissions ?? [], cancellationToken);
        return ApiResults.Ok(role);
    }
}
