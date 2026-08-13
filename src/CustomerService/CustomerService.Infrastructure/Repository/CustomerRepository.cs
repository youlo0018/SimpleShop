using CommunalService.Domain.Infrastructure;
using CustomerService.Domain.Entity;
using CustomerService.Domain.IRepository;

namespace CustomerService.Infrastructure.Repository;

public class CustomerRepository(IFreeSql freeSql) : BaseRepository<Customer>(freeSql), ICustomerRepository<Customer>
{
}