using CommunalService.Domain.Infrastructure;
using ProductService.Domain.Entity;
using ProductService.Domain.IRepository;

namespace ProductService.Infrastructure.Repository;

public class CategoryRepository(IFreeSql freeSql)
    : BaseRepository<Category>(freeSql), ICategoryRepository<Category>
{
}
