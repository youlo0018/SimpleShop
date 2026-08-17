using CommunalService.Domain;
using CustomerService.Application.Features.Customer.CreateCustomer;
using CustomerService.Application.Features.Customer.GetCustomer;
using CustomerService.Domain.Entity;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CustomerService.Api.Controllers;

public class CustomerController(IMediator mediator,IFreeSql freeSql) : BaseController
{
   

    [HttpPost]
    public async Task<ApiResponse> AddCustomer([FromBody]CreateCustomerCommand command)
    {
        //freeSql.CodeFirst.SyncStructure<Customer>();
        
        var data = await mediator.Send(command, CancellationToken.None);
        return Ok(data);
        
       
    }
    [HttpGet]
    public async Task<ApiResponse> GetCustomer([FromQuery]GetCustomerCommand command)
    {
        freeSql.CodeFirst.SyncStructure<Customer>();
        
        var data = await mediator.Send(command, CancellationToken.None);
        return Ok(data);
        
       
    }
}