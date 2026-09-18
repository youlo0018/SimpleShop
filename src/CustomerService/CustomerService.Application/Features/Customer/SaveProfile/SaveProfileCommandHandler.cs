using CommunalService.Domain;
using CommunalService.Domain.Enums;
using CustomerService.Application.Common;
using CustomerService.Domain.IRepository;
using MediatR;

namespace CustomerService.Application.Features.Customer.SaveProfile;

/// <summary>客户资料编辑：只允许修改本人资料（X-Claim-UserId），登录名/平台/密码不在此接口。</summary>
public class SaveProfileCommandHandler(ICustomerAccountRepository repository, TenantContext tenant)
    : IRequestHandler<SaveProfileCommand, ApiResponse>
{
    /// <summary>保存本人资料并返回最新资料。</summary>
    public async Task<ApiResponse> Handle(SaveProfileCommand request, CancellationToken cancellationToken)
    {
        if (tenant.UserId <= 0)
            return ApiResults.Fail(BaseApiResponseCode.Unauthorized, "请先登录");

        var customer = await repository.GetByIdAsync(tenant.UserId);
        if (customer is null || customer.IsDeleted)
            return ApiResults.Fail(BaseApiResponseCode.NotFound, "客户不存在");

        customer.Avatar = request.Avatar ?? string.Empty;
        customer.Gender = request.Gender;
        customer.Birth = request.Birth;
        customer.Email = request.Email ?? string.Empty;
        customer.Phone = request.Phone ?? string.Empty;
        await repository.UpdateAsync(customer);
        return ApiResults.Ok(CustomerShaper.Shape(customer));
    }
}
