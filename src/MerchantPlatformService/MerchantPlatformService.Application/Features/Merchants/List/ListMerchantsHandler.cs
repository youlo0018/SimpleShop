using CommunalService.Domain;
using MerchantPlatformService.Domain.IRepository;
using MediatR;

namespace MerchantPlatformService.Application.Features.Merchants.List;

/// <summary>商户分页列表：平台账号只看本平台，商户账号只看本商户（数据隔离在 Handler 内完成）。</summary>
public sealed class ListMerchantsHandler(IMerchantRepository repository, TenantContext tenant)
    : IRequestHandler<ListMerchantsQuery, object>
{
    /// <summary>按登录态裁剪平台维度后分页查询。</summary>
    public async Task<object> Handle(ListMerchantsQuery request, CancellationToken cancellationToken)
    {
        var platformId = request.PlatformId;
        if (tenant.IsPlatform) platformId = tenant.PlatformId;
        var (items, total) = await repository.QueryPagedAsync(request.Keyword, request.Status, platformId, request.Page, request.PageSize);
        if (tenant.IsMerchant) items = items.Where(item => item.Id == tenant.MerchantId).ToList();
        return new { items, total, page = request.Page, pageSize = request.PageSize };
    }
}
