using CommunalService.Domain.Entity;
using CommunalService.Domain.Interfaces;
using ProductService.Domain.Entity;

namespace ProductService.Domain.IRepository;

public interface IProductRepository<T> : IBaseRepository<T> where T : BaseEntity
{
}

public interface ICategoryRepository<T> : IBaseRepository<T> where T : BaseEntity
{
}

public interface IBrandRepository<T> : IBaseRepository<T> where T : BaseEntity
{
}

public interface ISkuRepository<T> : IBaseRepository<T> where T : BaseEntity
{
}
