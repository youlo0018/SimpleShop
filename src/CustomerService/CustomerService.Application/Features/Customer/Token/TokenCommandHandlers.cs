using CommunalService.Domain;
using CommunalService.Domain.Enums;
using CustomerService.Application.Common;
using CustomerService.Application.Services;
using CustomerService.Domain.IRepository;
using MediatR;

namespace CustomerService.Application.Features.Customer.Token;

/// <summary>刷新令牌：校验 Redis 会话 → 换发新令牌（旧 jti 失效）。</summary>
public class RefreshTokenCommandHandler(
    ICustomerAccountRepository repository,
    CustomerTokenService tokenService,
    TenantContext tenant) : IRequestHandler<RefreshTokenCommand, ApiResponse>
{
    /// <summary>刷新当前登录客户的令牌；会话不存在时要求重新登录。</summary>
    public async Task<ApiResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        if (tenant.UserId <= 0) return ApiResults.Fail(BaseApiResponseCode.Unauthorized, "请先登录");
        var customer = await repository.GetByIdAsync(tenant.UserId);
        if (customer is null || customer.IsDeleted) return ApiResults.Fail(BaseApiResponseCode.Unauthorized, "请先登录");

        var token = await tokenService.RefreshAsync(tenant.Jti, customer);
        return token is null
            ? ApiResults.Fail(BaseApiResponseCode.Unauthorized, "登录已过期，请重新登录")
            : ApiResults.Ok(new { token, user = CustomerShaper.Shape(customer) });
    }
}

/// <summary>登出：删除当前令牌的 Redis 会话（服务端即时失效）。</summary>
public class LogoutCommandHandler(CustomerTokenService tokenService, TenantContext tenant)
    : IRequestHandler<LogoutCommand, ApiResponse>
{
    /// <summary>删除当前 jti 会话；未登录也返回成功，保证登出幂等。</summary>
    public async Task<ApiResponse> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        await tokenService.RevokeAsync(tenant.Jti);
        return ApiResults.Ok(new { success = true });
    }
}
