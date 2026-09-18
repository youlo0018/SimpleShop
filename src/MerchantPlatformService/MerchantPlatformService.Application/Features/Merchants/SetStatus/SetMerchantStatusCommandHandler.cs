using CommunalService.Domain;
using CommunalService.Domain.Enums;
using MediatR;
using MerchantPlatformService.Domain.IRepository;

namespace MerchantPlatformService.Application.Features.Merchants.SetStatus;

/// <summary>
/// 商户启停/状态变更：平台账号限本平台；状态枚举见 MerchantStatus。
/// </summary>
public class SetMerchantStatusCommandHandler(IMerchantRepository repository, TenantContext tenant)
    : IRequestHandler<SetMerchantStatusCommand, ApiResponse>
{
    /// <summary>处理入口：商户启停/状态变更：平台账号限本平台；状态枚举见 MerchantStatus。</summary>
    public async Task<ApiResponse> Handle(SetMerchantStatusCommand request, CancellationToken cancellationToken)
    {
        var merchant = await repository.GetByIdAsync(request.Id);
        if (merchant is null || merchant.IsDeleted || (tenant.IsPlatform && merchant.PlatformId != tenant.PlatformId))
            return ApiResults.Fail(BaseApiResponseCode.NotFound, "商户不存在");

        merchant.Status = request.Status;
        await repository.UpdateAsync(merchant, cancellationToken);
        return ApiResults.Ok(new { success = true });
    }
}
