using CommunalService.Domain.Infrastructure;
using MerchantPlatformService.Domain.Entity;
using MerchantPlatformService.Domain.IRepository;

namespace MerchantPlatformService.Infrastructure.Repository;

public class PlatformRepository(IFreeSql freeSql) : BaseRepository<Platform>(freeSql), IPlatformRepository
{

    public async Task<(List<Platform> Items, long Total)> QueryPagedAsync(string keyword, long? scopePlatformId,
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var selection = freeSql.Select<Platform>()
            .Where(platform => !platform.IsDeleted)
            .WhereIf(!string.IsNullOrWhiteSpace(keyword), platform =>
                platform.PlatformCode.Contains(keyword) || platform.PlatformName.Contains(keyword))
            .WhereIf(scopePlatformId.HasValue, platform => platform.Id == scopePlatformId!.Value);
        var total = await selection.CountAsync();
        var items = await selection.OrderByDescending(platform => platform.CreatedAt)
            .Page(Math.Max(page, 1), pageSize).ToListAsync();
        return (items, total);
    }

    public Task<long?> GetPlatformIdByMerchantAsync(long merchantId, CancellationToken cancellationToken = default)
        => freeSql.Select<Merchant>()
            .Where(merchant => merchant.Id == merchantId && !merchant.IsDeleted)
            .FirstAsync(merchant => (long?)merchant.PlatformId);
}
