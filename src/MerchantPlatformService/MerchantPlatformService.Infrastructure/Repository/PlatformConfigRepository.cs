using CommunalService.Domain.Infrastructure;
using MerchantPlatformService.Domain.Entity;
using MerchantPlatformService.Domain.IRepository;

namespace MerchantPlatformService.Infrastructure.Repository;

    /// <summary>平台配置仓储实现：按平台/配置键读写（Upsert 语义）。</summary>
    public class PlatformConfigRepository(IFreeSql freeSql) : BaseRepository<PlatformConfig>(freeSql), IPlatformConfigRepository
    {
        /// <summary>更新：UpdateAsync。</summary>
        public async Task<bool> UpdateAsync(PlatformConfig config, CancellationToken cancellationToken = default)
        {
            return await freeSql.Update<PlatformConfig>().SetSource(config)
                .ExecuteAffrowsAsync(cancellationToken) > 0;
        }

        /// <summary>存在则更新、否则新增（键为平台/配置键）。</summary>
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
