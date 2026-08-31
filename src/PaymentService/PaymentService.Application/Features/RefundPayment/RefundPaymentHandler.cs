using CommunalService.Domain;
using CommunalService.Domain.Enums;
using CommunalService.Domain.Infrastructure.Locks;
using MediatR;
using PaymentService.Domain.Entity;
using PaymentService.Domain.IRepository;

namespace PaymentService.Application.Features.RefundPayment;

public sealed class RefundPaymentHandler(
    IPaymentOrderRepository repository,
    TenantContext tenant,
    IDistributedLock distributedLock)
    : IRequestHandler<RefundPaymentCommand, ApiResponse>
{
    public async Task<ApiResponse> Handle(RefundPaymentCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "退款金额必须大于0");
        }

        await using var lockHandle = await distributedLock.AcquireAsync(
            $"lock:payment:refund:{request.BizNo}",
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(2),
            cancellationToken);
        if (lockHandle is null)
        {
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "退款处理中，请稍后重试");
        }

        var payment = await repository.GetByBizNoAsync(request.BizNo, cancellationToken);
        if (payment is null || payment.Status != 20)
        {
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "支付单不存在或未支付");
        }

        // 租户归属校验：客户仅限本人，平台/商户按租户范围。
        var inScope = tenant.HasWildcard
            || (tenant.IsPlatform && payment.PlatformId == tenant.PlatformId)
            || (tenant.IsMerchant && payment.MerchantId == tenant.MerchantId)
            || (tenant.IsCustomer && payment.UserId == tenant.UserId);
        if (!inScope)
        {
            return ApiResults.Fail(BaseApiResponseCode.Forbidden, "无权操作该支付单");
        }

        var committed = await repository.GetCommittedRefundAmountAsync(payment.Id, cancellationToken);
        if (committed + request.Amount > payment.Amount)
        {
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "累计退款不能超过实付金额");
        }

        var refund = new RefundOrder
        {
            RefundNo = $"RF{DateTimeOffset.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}",
            PaymentId = payment.Id,
            BizNo = payment.BizNo,
            PlatformId = payment.PlatformId,
            MerchantId = payment.MerchantId,
            UserId = payment.UserId,
            Amount = request.Amount,
            Reason = request.Reason,
            Status = 10
        };
        var added = await repository.AddRefundAsync(refund, cancellationToken);
        if (!added)
        {
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "退款创建失败");
        }

        if (request.Items.Count > 0)
        {
            await repository.AddRefundItemsAsync(refund.Id, payment.BizNo,
                request.Items.Select(item => (item.SkuId, item.Quantity)).ToList(), cancellationToken);
        }

        return ApiResults.Ok(new { success = true, refund.RefundNo, refund.Amount, refund.Status });
    }
}
