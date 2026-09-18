using CommunalService.Domain;
using CommunalService.Domain.Enums;
using CustomerService.Application.Common;
using CustomerService.Domain.IRepository;
using MediatR;

namespace CustomerService.Application.Features.Customer.GetProfile;

/// <summary>客户资料：只允许查当前登录客户，防止越权拉取他人资料。</summary>
public class GetProfileQueryHandler(ICustomerAccountRepository repository, TenantContext tenant)
    : IRequestHandler<GetProfileQuery, ApiResponse>
{
    /// <summary>按 X-Claim-UserId 查询本人资料。</summary>
    public async Task<ApiResponse> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        if (tenant.UserId <= 0)
            return ApiResults.Fail(BaseApiResponseCode.Unauthorized, "请先登录");
        var customer = await repository.GetByIdAsync(tenant.UserId);
        if (customer is null || customer.IsDeleted)
            return ApiResults.Fail(BaseApiResponseCode.NotFound, "客户不存在");
        return ApiResults.Ok(CustomerShaper.Shape(customer));
    }
}
