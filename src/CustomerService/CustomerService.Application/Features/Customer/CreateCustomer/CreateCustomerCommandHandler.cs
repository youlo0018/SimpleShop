using System.Security.Cryptography;
using Argon2id.PasswordHasher;
using CommunalService.Application.Common;
using CommunalService.Domain.Enums;
using CustomerService.Application.Mappers;
using CustomerService.Domin.IRepository;
using MediatR;
using Yitter.IdGenerator;
namespace CustomerService.Application.Features.Customer.CreateCustomer;

public class CreateCustomerCommandHandler(ICustomerRepository<Domin.Entity.Customer> customerRepository)
    : IRequestHandler<CreateCustomerCommand, object>
{
    public async Task<object> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        var custormer = request.CreateCustomer();
        custormer.Salt= Convert.ToBase64String(Tools.GenerateSalt(16)); 
        var hasher = new Argon2idPasswordHasher();
        custormer.pwd=hasher.HashPassword(custormer.Salt+custormer.pwd);
        custormer.CustomeNo =CusromerSourceCode.GetSource(custormer.RegisterSource).Code+YitIdHelper.NextId();
        
       await customerRepository.InsertAsync(custormer);
       return custormer.Id;
    }
}