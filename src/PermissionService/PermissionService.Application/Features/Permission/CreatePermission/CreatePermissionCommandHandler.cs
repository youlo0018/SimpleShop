using CommunalService.Domain;
using CommunalService.Domain.Enums;
using MediatR;
using PermissionEntity = PermissionService.Domain.Entity.Permission;
using PermissionService.Domain.IRepository;

namespace PermissionService.Application.Features.Permission.CreatePermission;

/// <summary>
/// 新增权限点：编码/路径查重；路径必须以 /gateway/ 开头（网关按它映射所需权限码，30s 缓存生效）。
/// </summary>
public class CreatePermissionCommandHandler(IPermissionCenterRepository repository)
    : IRequestHandler<CreatePermissionCommand, ApiResponse>
{
    public async Task<ApiResponse> Handle(CreatePermissionCommand request, CancellationToken cancellationToken)
    {
        if (await repository.PermissionExistsAsync(request.Code, request.InterfacePath, cancellationToken))
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "权限编码或接口路径已存在");

        var permission = new PermissionEntity
        {
            Code = request.Code.Trim(), Name = request.Name.Trim(), Resource = request.Resource,
            Action = request.Action, InterfacePath = request.InterfacePath.Trim(), AllowedScopes = request.AllowedScopes
        };
        await repository.InsertPermissionAsync(permission, cancellationToken);
        return ApiResults.Ok(permission);
    }
}
