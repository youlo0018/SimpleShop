using CommunalService.Domain;
using CustomerService.Application.Features.Customer.CreateCustomer;
using CustomerService.Domin.Entity;
using CustomerService.Domin.IRepository;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CustomerService.Api.Controllers;

public class CustomerController(IMediator mediator,IFreeSql freeSql) : BaseController
{
   

    [HttpPost]
    public async Task<BaseApiResponse> AddCustomer([FromBody]CreateCustomerCommand command)
    {
        freeSql.CodeFirst.SyncStructure<Customer>();
        
        var data = await mediator.Send(command);
        return Ok(data);
        
       
    }
}