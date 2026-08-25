using CommunalService.Domain.Infrastructure;
using MerchantPlatformService.Domain.Entity;
using MerchantPlatformService.Domain.IRepository;

namespace MerchantPlatformService.Infrastructure.Repository;

    public class PlatformConfigRepository(IFreeSql freeSql) : BaseRepository<PlatformConfig>(freeSql), IPlatformConfigRepository
    {
        public async Task<bool> UpdateAsync(PlatformConfig config, CancellationToken cancellationToken = default)
        {
            return await freeSql.Update<PlatformConfig>().SetSource(config)
                .ExecuteAffrowsAsync(cancellationToken) > 0;
        }

        public async Task<bool> UpsertAsync(PlatformConfig config, CancellationToken cancellationToken = default)
    {
        var existing = (await QueryAsync(item => item.ConfigKey == config.ConfigKey)).FirstOrDefault();
        if (existing is null)
        {
            return await InsertAsync(config);
        }

        config.Id = existing.Id;
        return await UpdateAsync(config, cancellationToken);
    }
}
