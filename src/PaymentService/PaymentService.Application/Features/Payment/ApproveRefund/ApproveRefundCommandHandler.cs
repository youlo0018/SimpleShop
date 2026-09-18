using CommunalService.Domain;
using CommunalService.Domain.Enums;
using CommunalService.Domain.Messaging;
using MediatR;
using PaymentService.Domain.IRepository;

namespace PaymentService.Application.Features.Payment.ApproveRefund;

/// <summary>
/// 同意退款：状态机校验（仅待审批 10）→ 标记已退款并累计金额 → 计算是否全额退款 →
/// 发布 payment.refunded 事件（订单转已退款、库存按退款单号幂等回补）。平台通吃，商户限本商户。
/// </summary>
public class ApproveRefundCommandHandler(
    IPaymentOrderRepository repository,
    TenantContext tenant,
    IMessagePublisher messagePublisher) : IRequestHandler<ApproveRefundCommand, ApiResponse>
{
    /// <summary>处理入口：同意退款：状态机校验（仅待审批 10）→ 标记已退款并累计金额 → 计算是否全额退款 → 发布 payment.refunded 事件（订单转已退款、库存按退款单号幂等回补）。平台通吃，商户限本商户。</summary>
    public async Task<ApiResponse> Handle(ApproveRefundCommand request, CancellationToken cancellationToken)
    {
        var refund = await repository.GetRefundByIdAsync(request.Id, cancellationToken);
        if (refund is null) return ApiResults.Fail(BaseApiResponseCode.NotFound, "退款单不存在");
        if (!CanDecide(refund)) return ApiResults.Fail(BaseApiResponseCode.Forbidden, "无权审批该退款单");
        if (refund.Status != 10) return ApiResults.Fail(BaseApiResponseCode.BadRequest, "当前退款状态不可审批");

        var updated = await repository.MarkRefundedAsync(refund.RefundNo, cancellationToken);
        if (!updated) return ApiResults.Fail(BaseApiResponseCode.BadRequest, "退款状态更新失败");

        var refundItems = await repository.ListRefundItemsAsync(refund.Id, cancellationToken);
        var isAllRefund = await repository.GetRefundedAmountAsync(refund.PaymentId, cancellationToken)
            >= (await repository.GetByIdAsync(refund.PaymentId, cancellationToken))!.Amount;

        await messagePublisher.PublishAsync("payment.refunded", refund.RefundNo,
            new MessageEnvelope<object>(Guid.NewGuid(), "payment.refunded", DateTimeOffset.UtcNow, refund.RefundNo,
                refund.PlatformId, refund.MerchantId, refund.UserId, 1,
                new
                {
                    bizNo = refund.BizNo, refundNo = refund.RefundNo, amount = refund.Amount, isAllRefund,
                    stockItems = refundItems.Select(item => new { item.SkuId, item.Quantity })
                }), cancellationToken);
        return ApiResults.Ok(new { success = true });
    }

    /// <summary>条件判断：CanDecide。</summary>
    private bool CanDecide(Domain.Entity.RefundOrder refund)
        => tenant.HasWildcard || tenant.IsPlatform || (tenant.IsMerchant && refund.MerchantId == tenant.MerchantId);
}
