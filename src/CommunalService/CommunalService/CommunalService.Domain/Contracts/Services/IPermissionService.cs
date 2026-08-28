using CommunalService.Domain.Contracts.Messages;
using MagicOnion;

namespace CommunalService.Domain.Contracts.Services;

/// <summary>
/// 权限中心服务间契约；UserService 登录时必须通过该接口解析账号权限，禁止直接访问权限库。
/// </summary>
public interface IPermissionService : IService<IPermissionService>
{
    UnaryResult<AuthorizationResponse> ResolveAsync(AuthorizationRequest request);

    /// <summary>后台账号创建/修改时绑定权限中心中的真实角色；customer 表示清除后台角色绑定。</summary>
    UnaryResult<bool> AssignRoleAsync(AssignRoleRequest request);

    /// <summary>返回启用中的权限目录；网关用它把动态配置的接口路径映射为所需权限编码。</summary>
    UnaryResult<List<PermissionCatalogItem>> ListCatalogAsync();
}
