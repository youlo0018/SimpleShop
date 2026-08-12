using System.ComponentModel;
using MediatR;


namespace CustomerService.Application.Features.Customer.CreateCustomer;

public record CreateCustomerCommand:IRequest<object>
{
    [Description("用户名")]
    public string CustomerName { get; set; }
    [Description("邮箱")]
    public string Email { get; set; }
    [Description("手机号")]
    public string Phone { get; set; }
    [Description("性别")]
    public int Gender  { get; set; }
    [Description("生日")]
    public DateTime  Birth { get; set; }
    [Description("是否同意所有用户协议")]
    public bool IsAllAgreeAgreement{get; set;}
    [Description("注册来源")]
    public int RegisterSource {get; set;}
    [Description("密码")]
    public string pwd { get; set; }
}