using CommunalService.Domain;
using CommunalService.Domain.Enums;
using CustomerService.Application.Common;
using CustomerService.Application.Services;
using CustomerService.Domain.Enums;
using CustomerService.Domain.IRepository;
using MediatR;
using CustomerEntity = CustomerService.Domain.Entity.Customer;

namespace CustomerService.Application.Features.Customer.Register;

/// <summary>
/// 客户注册：平台内查重（登录名/手机号）→ 生成盐并散列落库（记录协议与来源）→ 签发客户 JWT 自动登录。
/// 注册只产生 customer 身份，后台账号由 UserService 管理，两者账号体系完全分离。
/// </summary>
public class RegisterCommandHandler(
    ICustomerAccountRepository repository,
    CustomerTokenService tokenService) : IRequestHandler<RegisterCommand, ApiResponse>
{
    /// <summary>注册并自动登录：成功返回 token 与客户资料（含所属平台）。</summary>
    public async Task<ApiResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (await repository.ExistsAsync(request.PlatformId, request.UserName, request.Phone))
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "用户名或手机号已存在");

        var salt = PasswordHasher.NewSalt();
        var customer = new CustomerEntity
        {
            PlatformId = request.PlatformId,
            CustomeNo = $"C{DateTime.Now:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}",
            CustomerName = request.UserName.Trim(),
            Email = request.Email ?? string.Empty,
            Phone = request.Phone ?? string.Empty,
            pwd = PasswordHasher.Hash(request.Password, salt),
            Salt = salt,
            IsAllAgreeAgreement = request.AgreedAgreement,
            RegisterSource = request.RegisterSource == 0 ? (int)RegisterSource.MiniApp : request.RegisterSource
        };
        if (!await repository.InsertAsync(customer))
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "注册失败，请稍后重试");

        return ApiResults.Ok(new { token = await tokenService.IssueAsync(customer), user = CustomerShaper.Shape(customer) });
    }
}
