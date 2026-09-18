using CommunalService.Domain;
using CommunalService.Domain.Enums;
using MediatR;
using MerchantPlatformService.Domain.IRepository;

namespace MerchantPlatformService.Application.Features.Platforms.List;

/// <summary>
/// 平台分页列表：平台账号只看自己平台，商户账号经 Merchant 表反查所在平台后只看该平台。
/// </summary>
public class ListPlatformsQueryHandler(IPlatformRepository repository, TenantContext tenant)
    : IRequestHandler<ListPlatformsQuery, ApiResponse>
{
    /// <summary>处理入口：平台分页列表：平台账号只看自己平台，商户账号经 Merchant 表反查所在平台后只看该平台。</summary>
    public async Task<ApiResponse> Handle(ListPlatformsQuery request, CancellationToken cancellationToken)
    {
        // 平台管理员看所有平台；平台账号只看自己；商户账号看自己入驻的平台。
        long? scopePlatformId = tenant.IsPlatform ? tenant.PlatformId : null;
        if (tenant.IsMerchant)
            scopePlatformId = await repository.GetPlatformIdByMerchantAsync(tenant.MerchantId, cancellationToken);

        var (items, total) = await repository.QueryPagedAsync(
            request.Keyword, scopePlatformId, request.Page, request.PageSize, cancellationToken);
        return ApiResults.Ok(new { items, total, page = request.Page, pageSize = request.PageSize });
    }
}
