using CommunalService.Domain;
using Microsoft.AspNetCore.Http;
using CommunalService.Domain.Enums;
using CommunalService.Domain.Infrastructure.Locks;
using CommunalService.Domain.Logging;
using CommunalService.Domain.Messaging;
using MediatR;
using PaymentService.Domain.IRepository;

namespace PaymentService.Application.Features.ConfirmPayment;

/// <summary>确认支付：状态幂等 → 标记已支付 → 发 payment.succeeded（含库存明细）。</summary>
public sealed class ConfirmPaymentHandler(
    IPaymentOrderRepository repository,
    TenantContext tenant,
    IDistributedLock distributedLock,
    IMessagePublisher messagePublisher,
    IHttpContextAccessor httpContextAccessor,
    IOperationLogger operationLogger)
    : IRequestHandler<ConfirmPaymentCommand, ApiResponse>
{
    /// <summary>处理入口：确认支付：状态幂等 → 标记已支付 → 发 payment.succeeded（含库存明细）。</summary>
    public async Task<ApiResponse> Handle(ConfirmPaymentCommand request, CancellationToken cancellationToken)
    {
        await using var lockHandle = await distributedLock.AcquireAsync(
            $"lock:payment:callback:{request.BizNo}",
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(2),
            cancellationToken);

        if (lockHandle is null)
        {
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "支付回调处理中，请稍后重试");
        }

        var payment = await repository.GetByBizNoAsync(request.BizNo, cancellationToken);
        if (payment is null)
        {
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "支付单不存在");
        }

        // 客户只能确认自己的支付单；后台租户（平台/商户）不受此限制。
        if (tenant.IsCustomer && payment.UserId != tenant.UserId)
        {
            return ApiResults.Fail(BaseApiResponseCode.Forbidden, "无权确认该支付单");
        }

        if (payment.Status == 20)
        {
            // 幂等确认不只是返回旧结果：如果上次“落库成功、发事件失败”，这里必须补发。
            // 订单和库存消费方都按状态/流水幂等，重复事件是安全的。
            await PublishSucceededAsync(payment, request.Items, cancellationToken);
            return ApiResults.Ok(new { success = true, idempotent = true, payment.PaymentNo });
        }

        if (payment.Status != 10)
        {
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "支付单状态不可确认");
        }

        var paid = await repository.MarkPaidAsync(payment.Id, cancellationToken);
        if (!paid)
        {
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "支付状态更新失败");
        }

        // 支付成功是全链路的“发令枪”：订单、库存、履约都靠这个事件推进。
        await PublishSucceededAsync(payment, request.Items, cancellationToken);

        // 资金状态变更后必须留审计；日志发布失败由 OperationLogger 内部降级，不影响业务结果。
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            await operationLogger.LogAsync(
                httpContext,
                "confirm",
                "payment",
                payment.PaymentNo,
                $"支付确认成功，业务单：{payment.BizNo}，金额：{payment.Amount:0.##}",
                cancellationToken);
        }

        return ApiResults.Ok(new { success = true, payment.PaymentNo, status = 20 });
    }

    /// <summary>辅助处理：PublishSucceededAsync。</summary>
    private async Task PublishSucceededAsync(
        Domain.Entity.PaymentOrder payment,
        IReadOnlyCollection<CreatePayment.PaymentStockItem> items,
        CancellationToken cancellationToken)
    {
        await messagePublisher.PublishAsync(
            "payment.succeeded",
            payment.BizNo,
            new MessageEnvelope<object>(
                Guid.NewGuid(),
                "payment.succeeded",
                DateTimeOffset.UtcNow,
                Guid.NewGuid().ToString("N"),
                payment.PlatformId,
                payment.MerchantId,
                payment.UserId,
                1,
                new
                {
                    bizNo = payment.BizNo,
                    paymentNo = payment.PaymentNo,
                    amount = payment.Amount,
                    stockItems = items.Select(item => new { item.SkuId, item.Quantity })
                }),
            cancellationToken);
    }
}
