using CommunalService.Domain;
using CommunalService.Domain.Enums;
using MediatR;
using PermissionService.Domain.Entity;
using PermissionService.Domain.IRepository;

namespace PermissionService.Application.Features.Permission.BindUser;

/// <summary>
/// 绑定用户角色：校验角色存在与租户范围（平台角色必须带平台、商户角色必须带商户）→
/// 一个用户只保留一条有效绑定（旧的软删，新的插入）。
/// </summary>
public class BindUserCommandHandler(IPermissionCenterRepository repository)
    : IRequestHandler<BindUserCommand, ApiResponse>
{
    /// <summary>处理入口：绑定用户角色：校验角色存在与租户范围（平台角色必须带平台、商户角色必须带商户）→ 一个用户只保留一条有效绑定（旧的软删，新的插入）。</summary>
    public async Task<ApiResponse> Handle(BindUserCommand request, CancellationToken cancellationToken)
    {
        var role = await repository.GetRoleAsync(request.RoleId, cancellationToken);
        if (role is null) return ApiResults.Fail(BaseApiResponseCode.NotFound, "角色不存在");
        if (role.TenantType == (int)TenantType.Platform && request.PlatformId <= 0)
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "平台角色必须绑定平台");
        if (role.TenantType == (int)TenantType.Merchant && request.MerchantId <= 0)
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "商户角色必须绑定商户");

        var old = await repository.GetUserBindingAsync(request.UserId, cancellationToken);
        if (old is not null)
            await repository.HardDeleteBindingAsync(old.Id, cancellationToken);
        await repository.InsertBindingAsync(new UserRole
        {
            UserId = request.UserId, RoleId = request.RoleId,
            PlatformId = request.PlatformId, MerchantId = request.MerchantId
        }, cancellationToken);
        return ApiResults.Ok(new { success = true });
    }
}
