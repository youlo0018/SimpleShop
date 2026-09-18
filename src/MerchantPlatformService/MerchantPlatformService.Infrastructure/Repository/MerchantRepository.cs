using CommunalService.Domain.Infrastructure;
using MerchantPlatformService.Domain.Entity;
using MerchantPlatformService.Domain.IRepository;

namespace MerchantPlatformService.Infrastructure.Repository;

/// <summary>商户仓储实现：分页组合查询与状态更新。</summary>
public class MerchantRepository(IFreeSql freeSql) : BaseRepository<Merchant>(freeSql), IMerchantRepository
{
    /// <summary>更新：UpdateAsync。</summary>
    public async Task<bool> UpdateAsync(Merchant merchant, CancellationToken cancellationToken = default)
    {
        return await freeSql.Update<Merchant>().SetSource(merchant)
            .ExecuteAffrowsAsync(cancellationToken) > 0;
    }

    /// <inheritdoc />
    public async Task<(List<Merchant> Items, long Total)> QueryPagedAsync(string keyword, int? status, long platformId, int page, int pageSize)
    {
        var selection = freeSql.Select<Merchant>()
            .Where(merchant => !merchant.IsDeleted)
            .WhereIf(!string.IsNullOrWhiteSpace(keyword), merchant =>
                merchant.MerchantName.Contains(keyword) || merchant.ContactName.Contains(keyword) || merchant.ContactPhone.Contains(keyword))
            .WhereIf(status.HasValue, merchant => merchant.Status == status!.Value)
            .WhereIf(platformId > 0, merchant => merchant.PlatformId == platformId);
        var total = await selection.CountAsync();
        var items = await selection.OrderByDescending(merchant => merchant.CreatedAt)
            .Page(Math.Max(page, 1), pageSize).ToListAsync();
        return (items, total);
    }
}
