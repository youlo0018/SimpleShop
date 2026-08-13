
using CustomerService.Domain.IRepository;
using MediatR;


namespace CustomerService.Application.Features.Customer.GetCustomer;

public class GetCustomerCommandHandler(ICustomerRepository<Domain.Entity.Customer> customerRepository)
    : IRequestHandler<GetCustomerCommand,object>
{
    public async Task<object> Handle(GetCustomerCommand request, CancellationToken cancellationToken)
    {
        var entity = await customerRepository.GetByIdAsync(request.Id);
        return entity;
    }
}