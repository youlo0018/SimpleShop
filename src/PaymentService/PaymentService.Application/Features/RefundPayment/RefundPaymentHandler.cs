using CommunalService.Domain.Infrastructure.Locks;
using CommunalService.Domain.Messaging;
using MediatR;
using PaymentService.Domain.IRepository;
using PaymentService.Domain.Entity;

namespace PaymentService.Application.Features.RefundPayment;

/// <summary>
/// 退款：支付单必须已成功；分布式锁避免同一笔支付并发退款导致累计金额超过实付。
/// </summary>
public sealed class RefundPaymentHandler(
    IPaymentOrderRepository repository,
    IFreeSql freeSql,
    IDistributedLock distributedLock,
    IMessagePublisher messagePublisher)
    : IRequestHandler<RefundPaymentCommand, object>
{
    public async Task<object> Handle(RefundPaymentCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            return new { success = false, message = "退款金额必须大于0" };
        }

        await using var lockHandle = await distributedLock.AcquireAsync(
            $"lock:payment:refund:{request.BizNo}",
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(2),
            cancellationToken);

        if (lockHandle is null)
        {
            return new { success = false, message = "退款处理中，请稍后重试" };
        }

        var payment = await repository.GetByBizNoAsync(request.BizNo, cancellationToken);
        if (payment is null || payment.Status != 20)
        {
            return new { success = false, message = "支付单不存在或未支付" };
        }

        var committed = await repository.GetCommittedRefundAmountAsync(payment.Id, cancellationToken);
        if (committed + request.Amount > payment.Amount)
        {
            return new { success = false, message = "累计退款不能超过实付金额" };
        }

        var refund = new Domain.Entity.RefundOrder
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
            return new { success = false, message = "退款创建失败" };
        }

        if (request.Items.Count > 0)
        {
            var refundItems = request.Items.Select(item => new RefundOrderItem
            {
                RefundId = refund.Id,
                BizNo = payment.BizNo,
                SkuId = item.SkuId,
                Quantity = item.Quantity
            }).ToList();
            await freeSql.Insert(refundItems).ExecuteAffrowsAsync(cancellationToken);
        }

        // 退款单进入待审核；同意后再发布退款事件，避免未审批就恢复库存或更新订单状态。
        return new { success = true, refundNo = refund.RefundNo, amount = refund.Amount, status = refund.Status };
    }
}
