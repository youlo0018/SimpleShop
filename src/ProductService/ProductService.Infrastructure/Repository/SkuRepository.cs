using CommunalService.Domain.Infrastructure;
using ProductService.Domain.Entity;
using ProductService.Domain.IRepository;

namespace ProductService.Infrastructure.Repository;

public class SkuRepository(IFreeSql freeSql)
    : BaseRepository<Sku>(freeSql), ISkuRepository<Sku>
{
}
