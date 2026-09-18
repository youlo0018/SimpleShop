using CommunalService.Domain;
using CommunalService.Domain.Enums;
using CustomerService.Domain.Entity;
using CustomerService.Domain.IRepository;
using MediatR;

namespace CustomerService.Application.Features.Address.SaveAddress;

/// <summary>
/// 新增/编辑地址：归属校验（编辑时地址必须属于当前客户）→ 落库 → 设默认时清除该客户其他默认标记。
/// </summary>
public class SaveAddressCommandHandler(ICustomerAddressRepository repository, TenantContext tenant)
    : IRequestHandler<SaveAddressCommand, ApiResponse>
{
    /// <summary>保存地址：客户只能操作自己的地址；默认地址全局唯一。</summary>
    public async Task<ApiResponse> Handle(SaveAddressCommand request, CancellationToken cancellationToken)
    {
        var customerId = tenant.UserId;
        if (customerId <= 0)
            return ApiResults.Fail(BaseApiResponseCode.Unauthorized, "请先登录");

        CustomerAddress address;
        if (request.Id > 0)
        {
            address = await repository.GetByIdAsync(request.Id);
            if (address is null || address.IsDeleted || address.CustomerId != customerId)
                return ApiResults.Fail(BaseApiResponseCode.NotFound, "地址不存在");
        }
        else
        {
            address = new CustomerAddress { CustomerId = customerId };
        }

        address.ReceiverName = request.ReceiverName;
        address.ReceiverPhone = request.ReceiverPhone;
        address.Province = request.Province;
        address.City = request.City;
        address.District = request.District;
        address.Detail = request.Detail;
        address.IsDefault = request.IsDefault;

        if (address.Id == 0)
            await repository.InsertAsync(address);
        else
            await repository.UpdateAsync(address);

        // 默认地址唯一化，避免下单页出现多个默认项。
        if (address.IsDefault)
            await repository.ClearDefaultAsync(address.CustomerId, address.Id);

        return ApiResults.Ok(address);
    }
}
