using CommunalService.Domain;
using CustomerService.Application.Features.Customer.CreateCustomer;
using CustomerService.Application.Features.Customer.GetCustomer;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CustomerService.Api.Controllers;

public class CustomerController(IMediator mediator,IFreeSql freeSql) : BaseController
{
   

    [HttpPost]
    public async Task<BaseApiResponse> AddCustomer([FromBody]CreateCustomerCommand command)
    {
        //freeSql.CodeFirst.SyncStructure<Customer>();
        
        var data = await mediator.Send(command, CancellationToken.None);
        return Ok(data);
        
       
    }
    [HttpGet]
    public async Task<BaseApiResponse> GetCustomer([FromQuery]GetCustomerCommand command)
    {
        //freeSql.CodeFirst.SyncStructure<Customer>();
        
        var data = await mediator.Send(command, CancellationToken.None);
        return Ok(data);
        
       
    }
}