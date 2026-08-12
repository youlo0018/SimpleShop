
using System.ServiceModel;
using CommunalService.Domain.Contracts.Messages;
using MagicOnion;


namespace CommunalService.Domain.Contracts.Services;


public interface ICustomerService: IService<ICustomerService>
{
    UnaryResult<GetCustomerResponse> GetCustomerAsync(GetCustomerRequest request);
}