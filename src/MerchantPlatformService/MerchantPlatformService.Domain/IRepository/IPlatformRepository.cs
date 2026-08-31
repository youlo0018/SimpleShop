using CommunalService.Domain.Interfaces;
using MerchantPlatformService.Domain.Entity;

namespace MerchantPlatformService.Domain.IRepository;

public interface IPlatformRepository : IBaseRepository<Platform>
{
    Task<(List<Platform> Items, long Total)> QueryPagedAsync(string keyword, long? scopePlatformId,
        int page, int pageSize, CancellationToken cancellationToken = default);

    Task<long?> GetPlatformIdByMerchantAsync(long merchantId, CancellationToken cancellationToken = default);
}
