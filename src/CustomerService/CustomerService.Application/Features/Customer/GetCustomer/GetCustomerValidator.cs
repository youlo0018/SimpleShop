using FluentValidation;

namespace CustomerService.Application.Features.Customer.GetCustomer;

public class GetCustomerValidator: AbstractValidator<GetCustomerCommand>
{
    public GetCustomerValidator()
    {
        RuleFor(v => v.Id).NotEmpty().GreaterThan(0).WithMessage("用户id不能为空");
    }
}