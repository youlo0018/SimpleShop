using CommunalService.Domain.Infrastructure;
using ProductService.Domain.Entity;
using ProductService.Domain.IRepository;

namespace ProductService.Infrastructure.Repository;

/// <summary>商品仓储实现（读侧）：商品与 SKU 查询。</summary>
public class ProductRepository(IFreeSql freeSql)
    : BaseRepository<Product>(freeSql), IProductRepository<Product>
{
}
