using CommunalService.Domain.Infrastructure;
using ProductService.Domain.Entity;
using ProductService.Domain.IRepository;

namespace ProductService.Infrastructure.Repository;

/// <summary>品牌仓储实现。</summary>
public class BrandRepository(IFreeSql freeSql)
    : BaseRepository<Brand>(freeSql), IBrandRepository<Brand>
{
}
