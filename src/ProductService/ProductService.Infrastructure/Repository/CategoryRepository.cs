using CommunalService.Domain.Infrastructure;
using ProductService.Domain.Entity;
using ProductService.Domain.IRepository;

namespace ProductService.Infrastructure.Repository;

/// <summary>分类仓储实现：分类树查询与层级校验。</summary>
public class CategoryRepository(IFreeSql freeSql)
    : BaseRepository<Category>(freeSql), ICategoryRepository<Category>
{
}
