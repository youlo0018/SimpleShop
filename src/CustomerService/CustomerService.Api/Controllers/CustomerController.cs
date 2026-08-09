using CommunalService.Domain;
using CustomerService.Domin.Entity;
using CustomerService.Domin.IRepository;
using Microsoft.AspNetCore.Mvc;

namespace CustomerService.Api.Controllers;

public class CustomerController(ICustomerRepository<Customer> customerRepository) : BaseController
{
    private readonly ICustomerRepository<Customer> _customerRepository = customerRepository;

    [HttpGet]
    public async Task<BaseApiResponse> AddCustomer<T>(Customer customer)
    {
        var list = await _customerRepository.QueryAsync(x => x.Id == 1);
        return Ok(list);
    }
}