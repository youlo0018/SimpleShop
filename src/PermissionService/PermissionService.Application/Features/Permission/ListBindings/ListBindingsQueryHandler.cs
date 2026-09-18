using CommunalService.Domain;
using MediatR;
using PermissionService.Domain.IRepository;

namespace PermissionService.Application.Features.Permission.ListBindings;

/// <summary>
/// 用户-角色绑定列表：联角色表补名称/编码/范围，供后台查看与解绑。
/// </summary>
public class ListBindingsQueryHandler(IPermissionCenterRepository repository)
    : IRequestHandler<ListBindingsQuery, ApiResponse>
{
    /// <summary>处理入口：用户-角色绑定列表：联角色表补名称/编码/范围，供后台查看与解绑。</summary>
    public async Task<ApiResponse> Handle(ListBindingsQuery request, CancellationToken cancellationToken)
    {
        var bindings = await repository.ListBindingsAsync(cancellationToken);
        var roles = await repository.ListRolesAsync(cancellationToken);
        return ApiResults.Ok(bindings.Select(binding =>
        {
            var role = roles.FirstOrDefault(item => item.Id == binding.RoleId);
            return new
            {
                binding.Id, binding.UserId, binding.RoleId,
                roleName = role?.Name ?? "",
                roleCode = role?.Code ?? "",
                roleTenantType = role?.TenantType ?? 0,
                binding.PlatformId, binding.MerchantId, binding.CreatedAt
            };
        }));
    }
}
