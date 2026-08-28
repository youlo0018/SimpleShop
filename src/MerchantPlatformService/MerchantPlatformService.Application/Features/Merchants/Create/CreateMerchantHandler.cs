using CommunalService.Domain.Logging;
using Microsoft.AspNetCore.Http;
using MediatR;
using MerchantPlatformService.Domain.Entity;
using MerchantPlatformService.Domain.Enums;
using MerchantPlatformService.Domain.IRepository;

namespace MerchantPlatformService.Application.Features.Merchants.Create;

public sealed class CreateMerchantHandler(
    IMerchantRepository merchantRepository,
    IPlatformRepository platformRepository,
    IHttpContextAccessor httpContextAccessor,
    IOperationLogger operationLogger) : IRequestHandler<CreateMerchantCommand, object>
{
    public async Task<object> Handle(CreateMerchantCommand request, CancellationToken cancellationToken)
    {
        var platform = await platformRepository.GetByIdAsync(request.PlatformId);
        if (platform is null || !platform.IsEnabled)
        {
            return new { success = false, message = "平台不存在或未启用，不能入驻" };
        }

        var merchant = new Merchant
        {
            PlatformId = request.PlatformId,
            MerchantNo = $"M{DateTimeOffset.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}",
            MerchantName = request.MerchantName,
            ContactName = request.ContactName,
            ContactPhone = request.ContactPhone,
            ContactEmail = request.ContactEmail,
            CommissionRate = request.CommissionRate,
            Status = (int)MerchantStatus.PendingReview
        };

        await merchantRepository.InsertAsync(merchant);

        // 入驻申请是商户进入平台的起点，后续审核和经营追溯都依赖这条审计记录。
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            await operationLogger.LogAsync(
                httpContext,
                "register",
                "merchant",
                merchant.MerchantNo,
                $"提交商户入驻：{merchant.MerchantName}，平台：{merchant.PlatformId}",
                cancellationToken);
        }

        return new { merchant.Id, merchant.MerchantNo, merchant.State };
    }
}
