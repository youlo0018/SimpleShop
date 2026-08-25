using CommunalService.Domain.Infrastructure;
using ProductService.Domain.Entity;
using ProductService.Domain.IRepository;

namespace ProductService.Infrastructure.Repository;

public class ProductRepository(IFreeSql freeSql)
    : BaseRepository<Product>(freeSql), IProductRepository<Product>
{
}
