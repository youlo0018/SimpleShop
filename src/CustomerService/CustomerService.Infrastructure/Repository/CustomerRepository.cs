using System.Linq.Expressions;
using CommunalService.Domain.Infrastructure;
using CommunalService.Domain.Interfaces;
using CustomerService.Domin.Entity;
using CustomerService.Domin.IRepository;

namespace CustomerService.Infrastructure.Repository;

public class CustomerRepository(IFreeSql freeSql):BaseRepository<Customer>(freeSql),ICustomerRepository  <Customer>
{


   
}