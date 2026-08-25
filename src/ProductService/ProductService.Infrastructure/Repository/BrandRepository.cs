using CommunalService.Domain.Infrastructure;
using ProductService.Domain.Entity;
using ProductService.Domain.IRepository;

namespace ProductService.Infrastructure.Repository;

public class BrandRepository(IFreeSql freeSql)
    : BaseRepository<Brand>(freeSql), IBrandRepository<Brand>
{
}
