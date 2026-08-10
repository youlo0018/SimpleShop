using CustomerService.Application.Features.Customer.CreateCustomer;
using CustomerService.Domin.Entity;

namespace CustomerService.Application.Mappers;

public static  class CustomerMapper
{
    public static Customer CreateCustomer(this CreateCustomerCommand command)
    {
        Customer customer = new Customer();
        customer.Email = command.Email;
        customer.Phone = command.Phone;
        customer.Birth = command.Birth;
        customer.CustomerName = command.CustomerName;
        customer.Gender = command.Gender;
        customer.IsAllAgreeAgreement = command.IsAllAgreeAgreement;
        customer.RegisterSource = command.RegisterSource;
        customer.pwd = command.pwd;
        return customer;

    }
}