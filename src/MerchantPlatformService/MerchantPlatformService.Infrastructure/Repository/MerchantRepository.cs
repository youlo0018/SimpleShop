using CommunalService.Domain.Infrastructure;
using MerchantPlatformService.Domain.Entity;
using MerchantPlatformService.Domain.IRepository;

namespace MerchantPlatformService.Infrastructure.Repository;

public class MerchantRepository(IFreeSql freeSql) : BaseRepository<Merchant>(freeSql), IMerchantRepository
{
    public async Task<bool> UpdateAsync(Merchant merchant, CancellationToken cancellationToken = default)
    {
        return await freeSql.Update<Merchant>().SetSource(merchant)
            .ExecuteAffrowsAsync(cancellationToken) > 0;
    }
}
