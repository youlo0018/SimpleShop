using CommunalService.Domain;
using CommunalService.Domain.Enums;
using MediatR;
using AddressEntity = UserService.Domain.Entity.Address;
using UserService.Domain.IRepository;

namespace UserService.Application.Features.Address.SaveAddress;

/// <summary>
/// 新增/编辑地址：归属校验（编辑时地址必须属于当前用户）→ 落库 → 设默认时清除该用户其他默认标记（唯一默认）。
/// </summary>
public class SaveAddressCommandHandler(IAddressRepository repository, TenantContext tenant)
    : IRequestHandler<SaveAddressCommand, ApiResponse>
{
    public async Task<ApiResponse> Handle(SaveAddressCommand request, CancellationToken cancellationToken)
    {
        if (!tenant.HasWildcard && !tenant.IsCustomer)
            return ApiResults.Fail(BaseApiResponseCode.Forbidden, "无权管理用户资料");
        var scopedUserId = tenant.HasWildcard ? request.UserId : tenant.UserId;
        if (scopedUserId <= 0)
            return ApiResults.Fail(BaseApiResponseCode.Unauthorized, "请先登录");

        AddressEntity address;
        if (request.Id > 0)
        {
            address = await repository.GetByIdAsync(request.Id);
            if (address is null || address.IsDeleted || address.UserId != scopedUserId)
                return ApiResults.Fail(BaseApiResponseCode.NotFound, "地址不存在");
        }
        else
        {
            address = new AddressEntity { UserId = scopedUserId };
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
            await repository.ClearDefaultAsync(address.UserId, address.Id);

        return ApiResults.Ok(address);
    }
}
