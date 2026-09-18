using CommunalService.Domain;
using CommunalService.Domain.Enums;
using CustomerService.Domain.IRepository;
using MediatR;

namespace CustomerService.Application.Features.Address.ListAddresses;

/// <summary>地址簿列表：默认地址排前；只允许查当前登录客户。</summary>
public class ListAddressesQueryHandler(ICustomerAddressRepository repository, TenantContext tenant)
    : IRequestHandler<ListAddressesQuery, ApiResponse>
{
    /// <summary>按 X-Claim-UserId 查询本人地址。</summary>
    public async Task<ApiResponse> Handle(ListAddressesQuery request, CancellationToken cancellationToken)
    {
        if (tenant.UserId <= 0)
            return ApiResults.Fail(BaseApiResponseCode.Unauthorized, "请先登录");
        return ApiResults.Ok(await repository.ListByCustomerAsync(tenant.UserId));
    }
}
