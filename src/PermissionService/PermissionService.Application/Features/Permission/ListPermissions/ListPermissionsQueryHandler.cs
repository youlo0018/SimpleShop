using CommunalService.Domain;
using MediatR;
using PermissionService.Domain.IRepository;

namespace PermissionService.Application.Features.Permission.ListPermissions;

/// <summary>
/// 启用中的权限目录（后台权限树数据源）；网关另经 gRPC ListCatalogAsync 消费同一数据做动态鉴权。
/// </summary>
public class ListPermissionsQueryHandler(IPermissionCenterRepository repository)
    : IRequestHandler<ListPermissionsQuery, ApiResponse>
{
    /// <summary>处理入口：启用中的权限目录（后台权限树数据源）；网关另经 gRPC ListCatalogAsync 消费同一数据做动态鉴权。</summary>
    public async Task<ApiResponse> Handle(ListPermissionsQuery request, CancellationToken cancellationToken)
        => ApiResults.Ok(await repository.ListEnabledPermissionsAsync(cancellationToken));
}
