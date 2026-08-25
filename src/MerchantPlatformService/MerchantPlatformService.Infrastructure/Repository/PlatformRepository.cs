using CommunalService.Domain.Infrastructure;
using MerchantPlatformService.Domain.Entity;
using MerchantPlatformService.Domain.IRepository;

namespace MerchantPlatformService.Infrastructure.Repository;

public class PlatformRepository(IFreeSql freeSql) : BaseRepository<Platform>(freeSql), IPlatformRepository
{
}
