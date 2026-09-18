using CommunalService.Domain;
using CommunalService.Domain.Enums;
using CustomerService.Domain.IRepository;
using MediatR;

namespace CustomerService.Application.Features.Address.DeleteAddress;

/// <summary>删除地址：软删除；客户只能删自己的地址。</summary>
public class DeleteAddressCommandHandler(ICustomerAddressRepository repository, TenantContext tenant)
    : IRequestHandler<DeleteAddressCommand, ApiResponse>
{
    /// <summary>删除本人地址；非本人地址按"地址不存在"处理，避免暴露他人数据。</summary>
    public async Task<ApiResponse> Handle(DeleteAddressCommand request, CancellationToken cancellationToken)
    {
        var customerId = tenant.UserId;
        if (customerId <= 0)
            return ApiResults.Fail(BaseApiResponseCode.Unauthorized, "请先登录");

        var address = await repository.GetByIdAsync(request.Id);
        if (address is null || address.IsDeleted || address.CustomerId != customerId)
            return ApiResults.Fail(BaseApiResponseCode.NotFound, "地址不存在");

        address.IsDeleted = true;
        address.DeletedAt = DateTime.Now;
        return await repository.UpdateAsync(address)
            ? ApiResults.Ok(new { success = true })
            : ApiResults.Fail(BaseApiResponseCode.NotFound, "地址不存在");
    }
}
