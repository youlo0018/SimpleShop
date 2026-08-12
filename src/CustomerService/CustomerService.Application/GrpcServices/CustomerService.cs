using CommunalService.Domain.Contracts.Messages;
using CommunalService.Domain.Contracts.Services;
using CustomerService.Application.Features.Customer.GetCustomer;
using MagicOnion;
using MagicOnion.Server;
using MediatR;
using YukeTools;

namespace CustomerService.Application.GrpcServices;

public class CustomerService(IMediator mediator): ServiceBase<ICustomerService>, ICustomerService
{
    public async UnaryResult<GetCustomerResponse> GetCustomerAsync(GetCustomerRequest request)
    {
       var data=await mediator.Send(new GetCustomerCommand() { Id = request.Id });
       return new GetCustomerResponse()
       {
           data = data.ToJson()
       };
    }
}