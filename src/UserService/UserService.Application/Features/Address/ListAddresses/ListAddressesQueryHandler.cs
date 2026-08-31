using CommunalService.Domain;
using CommunalService.Domain.Enums;
using MediatR;
using UserService.Domain.IRepository;

namespace UserService.Application.Features.Address.ListAddresses;

/// <summary>地址簿列表：默认地址排前；仅客户本人或后台 wildcard 可查。</summary>
public class ListAddressesQueryHandler(IAddressRepository repository, TenantContext tenant)
    : IRequestHandler<ListAddressesQuery, ApiResponse>
{
    public async Task<ApiResponse> Handle(ListAddressesQuery request, CancellationToken cancellationToken)
    {
        if (!tenant.HasWildcard && !tenant.IsCustomer)
            return ApiResults.Fail(BaseApiResponseCode.Forbidden, "无权查询用户资料");

        var scopedUserId = tenant.HasWildcard ? request.UserId : tenant.UserId;
        return ApiResults.Ok(await repository.ListByUserAsync(scopedUserId));
    }
}
