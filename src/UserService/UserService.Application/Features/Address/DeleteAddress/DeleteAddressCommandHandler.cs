using CommunalService.Domain;
using CommunalService.Domain.Enums;
using MediatR;
using UserService.Domain.IRepository;

namespace UserService.Application.Features.Address.DeleteAddress;

/// <summary>删除地址：软删除；客户只能删自己的地址。</summary>
public class DeleteAddressCommandHandler(IAddressRepository repository, TenantContext tenant)
    : IRequestHandler<DeleteAddressCommand, ApiResponse>
{
    public async Task<ApiResponse> Handle(DeleteAddressCommand request, CancellationToken cancellationToken)
    {
        if (!tenant.HasWildcard && !tenant.IsCustomer)
            return ApiResults.Fail(BaseApiResponseCode.Forbidden, "无权管理用户资料");

        var address = await repository.GetByIdAsync(request.Id);
        if (address is null || address.IsDeleted || (!tenant.HasWildcard && address.UserId != tenant.UserId))
            return ApiResults.Fail(BaseApiResponseCode.NotFound, "地址不存在");

        address.IsDeleted = true;
        address.DeletedAt = DateTime.Now;
        return await repository.UpdateAsync(address)
            ? ApiResults.Ok(new { success = true })
            : ApiResults.Fail(BaseApiResponseCode.NotFound, "地址不存在");
    }
}
