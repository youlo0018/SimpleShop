using MediatR;


namespace CustomerService.Application.Features.Customer.GetCustomer;

public record GetCustomerCommand:IRequest<object>
{
    public long Id { get; set; }
}