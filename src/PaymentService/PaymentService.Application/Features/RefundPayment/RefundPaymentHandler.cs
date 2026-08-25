using CommunalService.Domain.Infrastructure.Locks;
using MediatR;
using PaymentService.Domain.IRepository;

namespace PaymentService.Application.Features.RefundPayment;

/// <summary>
/// 退款：支付单必须已成功；分布式锁避免同一笔支付并发退款导致累计金额超过实付。
/// </summary>
public sealed class RefundPaymentHandler(
    IPaymentOrderRepository repository,
    IDistributedLock distributedLock)
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

        var refunded = await repository.GetRefundedAmountAsync(payment.Id, cancellationToken);
        if (refunded + request.Amount > payment.Amount)
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
            Status = 20,
            RefundedAt = DateTime.Now
        };

        var added = await repository.AddRefundAsync(refund, cancellationToken);
        return added
            ? new { success = true, refundNo = refund.RefundNo, amount = refund.Amount }
            : new { success = false, message = "退款创建失败" };
    }
}
