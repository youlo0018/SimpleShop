using CommunalService.Domain.Infrastructure;
using CustomerService.Domin.Entity;
using CustomerService.Domin.IRepository;

namespace CustomerService.Infrastructure.Repository;

public class CustomerRepository(IFreeSql freeSql) : BaseRepository<Customer>(freeSql), ICustomerRepository<Customer>
{
}