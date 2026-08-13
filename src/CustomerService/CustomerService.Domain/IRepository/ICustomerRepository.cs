using CommunalService.Domain.Entity;
using CommunalService.Domain.Interfaces;

namespace CustomerService.Domain.IRepository;

public interface ICustomerRepository<T>:IBaseRepository<T> where  T:BaseEntity
{
    
}