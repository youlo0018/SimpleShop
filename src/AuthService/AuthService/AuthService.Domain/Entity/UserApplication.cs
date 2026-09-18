using OpenIddict.EntityFrameworkCore.Models;

namespace AuthService.Domain.Entity;

/// <summary>
/// OpenIddict 客户端实体（默认实现即可）：公开/机密由 OpenIddict 自带 ClientType 决定，
/// 不再扩展业务分类列，避免遮蔽框架字段。
/// </summary>
public class UserApplication : OpenIddictEntityFrameworkCoreApplication<Guid, UserAuthorization, UserToken>
{
}
