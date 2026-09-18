using CommunalService.Domain;
using CommunalService.Domain.Enums;
using CustomerService.Application.Common;
using CustomerService.Application.Services;
using CustomerService.Domain.IRepository;
using MediatR;

namespace CustomerService.Application.Features.Customer.Login;

/// <summary>
/// 客户登录：按平台 + 登录名取账号 → 验密码（SHA256+盐）→ 校验未注销 → 签发客户 JWT。
/// </summary>
public class LoginCommandHandler(
    ICustomerAccountRepository repository,
    CustomerTokenService tokenService) : IRequestHandler<LoginCommand, ApiResponse>
{
    /// <summary>登录成功返回 token 与客户资料；失败统一提示"用户名或密码错误"避免账号枚举。</summary>
    public async Task<ApiResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var customer = await repository.GetByCustomerNameAsync(request.PlatformId, request.UserName);
        if (customer is null || customer.IsCancel || PasswordHasher.Hash(request.Password, customer.Salt) != customer.pwd)
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "用户名或密码错误");

        return ApiResults.Ok(new { token = await tokenService.IssueAsync(customer), user = CustomerShaper.Shape(customer) });
    }
}
