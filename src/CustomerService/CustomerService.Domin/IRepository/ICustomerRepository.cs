using CommunalService.Domain.Entity;
using CommunalService.Domain.Interfaces;

namespace CustomerService.Domin.IRepository;

public interface ICustomerRepository<T>:IBaseRepository<T> where  T:BaseEntity
{
    
}