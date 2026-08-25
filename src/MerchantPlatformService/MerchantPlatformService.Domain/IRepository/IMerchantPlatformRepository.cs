using CommunalService.Domain.Interfaces;
using CommunalService.Domain.Entity;
using MerchantPlatformService.Domain.Entity;

namespace MerchantPlatformService.Domain.IRepository;

public interface IMerchantRepository : IBaseRepository<Merchant>
{
    Task<bool> UpdateAsync(Merchant merchant, CancellationToken cancellationToken = default);
}

public interface IPlatformConfigRepository : IBaseRepository<PlatformConfig>
{
    Task<bool> UpdateAsync(PlatformConfig config, CancellationToken cancellationToken = default);
    Task<bool> UpsertAsync(PlatformConfig config, CancellationToken cancellationToken = default);
}
