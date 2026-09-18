using CommunalService.Domain.Infrastructure;
using ProductService.Domain.Entity;
using ProductService.Domain.IRepository;

namespace ProductService.Infrastructure.Repository;

/// <summary>SKU 仓储实现：按编码/商品查询。</summary>
public class SkuRepository(IFreeSql freeSql)
    : BaseRepository<Sku>(freeSql), ISkuRepository<Sku>
{
}
