using CommunalService.Domain.Logging;
using Microsoft.AspNetCore.Http;
using MediatR;
using CommunalService.Domain;
using MerchantPlatformService.Domain.Enums;
using MerchantPlatformService.Domain.IRepository;

namespace MerchantPlatformService.Application.Features.Merchants.Review;

public sealed class ReviewMerchantHandler(
    IMerchantRepository repository,
    TenantContext tenant,
    IHttpContextAccessor httpContextAccessor,
    IOperationLogger operationLogger) : IRequestHandler<ReviewMerchantCommand, object>
{
    public async Task<object> Handle(ReviewMerchantCommand request, CancellationToken cancellationToken)
    {
        var merchant = await repository.GetByIdAsync(request.Id);
        if (merchant is null || (tenant.IsPlatform && merchant.PlatformId != tenant.PlatformId) || (tenant.IsMerchant && merchant.Id != tenant.MerchantId))
        {
            return new { success = false, message = "无权操作该商户" };
        }

        if (merchant.State != MerchantStatus.PendingReview)
        {
            return new { success = false, message = "商户当前状态不可审核" };
        }

        merchant.Status = request.Approved ? (int)MerchantStatus.Approved : (int)MerchantStatus.Rejected;
        merchant.ReviewedAt = DateTime.Now;
        merchant.RejectReason = request.Approved ? null : request.Reason ?? "未通过审核";

        await repository.UpdateAsync(merchant, cancellationToken);

        // 审核结果决定商户能否经营，必须记录通过/驳回和驳回原因。
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            await operationLogger.LogAsync(
                httpContext,
                request.Approved ? "approve" : "reject",
                "merchant",
                merchant.MerchantNo,
                request.Approved
                    ? $"商户审核通过：{merchant.MerchantName}"
                    : $"商户审核驳回：{merchant.MerchantName}，原因：{merchant.RejectReason}",
                cancellationToken);
        }

        return new { success = true, merchant.Id, merchant.State, merchant.RejectReason };
    }
}
