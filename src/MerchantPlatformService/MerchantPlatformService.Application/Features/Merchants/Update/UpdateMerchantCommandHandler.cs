using CommunalService.Domain;
using CommunalService.Domain.Enums;
using MediatR;
using MerchantPlatformService.Domain.IRepository;

namespace MerchantPlatformService.Application.Features.Merchants.Update;

/// <summary>
/// 商户资料更新：平台账号限本平台商户；字段级校验见 UpdateMerchantValidator。
/// </summary>
public class UpdateMerchantCommandHandler(IMerchantRepository repository, TenantContext tenant)
    : IRequestHandler<UpdateMerchantCommand, ApiResponse>
{
    /// <summary>处理入口：商户资料更新：平台账号限本平台商户；字段级校验见 UpdateMerchantValidator。</summary>
    public async Task<ApiResponse> Handle(UpdateMerchantCommand request, CancellationToken cancellationToken)
    {
        var merchant = await repository.GetByIdAsync(request.Id);
        if (merchant is null || merchant.IsDeleted || (tenant.IsPlatform && merchant.PlatformId != tenant.PlatformId))
            return ApiResults.Fail(BaseApiResponseCode.NotFound, "商户不存在");

        merchant.MerchantName = request.MerchantName;
        merchant.ContactName = request.ContactName;
        merchant.ContactPhone = request.ContactPhone;
        merchant.ContactEmail = request.ContactEmail;
        merchant.CommissionRate = request.CommissionRate;
        await repository.UpdateAsync(merchant, cancellationToken);
        return ApiResults.Ok(new { success = true });
    }
}
