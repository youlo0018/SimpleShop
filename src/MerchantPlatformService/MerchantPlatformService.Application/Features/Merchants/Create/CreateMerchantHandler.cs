using CommunalService.Domain;
using CommunalService.Domain.Logging;
using Microsoft.AspNetCore.Http;
using MediatR;
using MerchantPlatformService.Domain.Entity;
using MerchantPlatformService.Domain.Enums;
using MerchantPlatformService.Domain.IRepository;

namespace MerchantPlatformService.Application.Features.Merchants.Create;

/// <summary>商户入驻：租户规则校验 → 生成商户编号（平台编码+雪花Id）→ 落库并记录审计。</summary>
public sealed class CreateMerchantHandler(
    IMerchantRepository merchantRepository,
    IPlatformRepository platformRepository,
    TenantContext tenant,
    IHttpContextAccessor httpContextAccessor,
    IOperationLogger operationLogger) : IRequestHandler<CreateMerchantCommand, object>
{
    /// <summary>处理入口：商户入驻：租户规则校验 → 生成商户编号（平台编码+雪花Id）→ 落库并记录审计。</summary>
    public async Task<object> Handle(CreateMerchantCommand request, CancellationToken cancellationToken)
    {
        // 租户规则：商户账号不能创建商户；平台账号只能在本平台下创建（回填平台）。
        if (tenant.IsMerchant) return new { success = false, message = "商户账号不能创建商户" };
        var platformId = tenant.IsPlatform ? tenant.PlatformId : request.PlatformId;
        var platform = await platformRepository.GetByIdAsync(platformId);
        if (platform is null || !platform.IsEnabled)
        {
            return new { success = false, message = "平台不存在或未启用，不能入驻" };
        }

        var merchant = new Merchant
        {
            PlatformId = platformId,
            MerchantName = request.MerchantName,
            ContactName = request.ContactName,
            ContactPhone = request.ContactPhone,
            ContactEmail = request.ContactEmail,
            CommissionRate = request.CommissionRate,
            Status = (int)MerchantStatus.PendingReview
        };

        await merchantRepository.InsertAsync(merchant);
        // 商户编号 = 平台编码 + 雪花 Id（插入后 Id 才生成），保证跨平台可读且全局唯一。
        merchant.MerchantNo = $"{platform.PlatformCode}{merchant.Id}";
        await merchantRepository.UpdateAsync(merchant);

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
