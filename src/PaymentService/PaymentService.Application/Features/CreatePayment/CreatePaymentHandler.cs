using CommunalService.Domain.Infrastructure.Locks;
using MediatR;
using PaymentService.Domain.Entity;
using PaymentService.Domain.IRepository;

namespace PaymentService.Application.Features.CreatePayment;

public sealed class CreatePaymentHandler(IPaymentOrderRepository repository, IDistributedLock distributedLock)
    : IRequestHandler<CreatePaymentCommand, object>
{
    public async Task<object> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
    {
        await using var lockHandle = await distributedLock.AcquireAsync(
            $"lock:payment:order:{request.BizNo}",
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(2),
            cancellationToken);

        if (lockHandle is null)
        {
            return new { success = false, message = "支付处理中，请稍后重试" };
        }

        var existing = await repository.GetByBizNoAsync(request.BizNo, cancellationToken);
        if (existing is not null)
        {
            return new { success = true, existing.Id, existing.PaymentNo, existing.Status };
        }

        var payment = new PaymentOrder
        {
            PaymentNo = $"P{DateTimeOffset.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(100000, 999999)}",
            BizNo = request.BizNo,
            PlatformId = request.PlatformId,
            MerchantId = request.MerchantId,
            UserId = request.UserId,
            Amount = request.Amount
        };

        return await repository.AddAsync(payment, cancellationToken)
            ? new { success = true, payment.Id, payment.PaymentNo }
            : new { success = false, message = "支付单创建失败" };
    }
}
