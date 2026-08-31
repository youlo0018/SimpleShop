using CommunalService.Domain;
using CommunalService.Domain.Enums;
using CommunalService.Domain.Infrastructure.Locks;
using MediatR;
using PaymentService.Domain.Entity;
using PaymentService.Domain.IRepository;

namespace PaymentService.Application.Features.CreatePayment;

public sealed class CreatePaymentHandler(
    IPaymentOrderRepository repository,
    TenantContext tenant,
    IDistributedLock distributedLock)
    : IRequestHandler<CreatePaymentCommand, ApiResponse>
{
    public async Task<ApiResponse> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
    {
        if (tenant.UserId <= 0)
        {
            return ApiResults.Fail(BaseApiResponseCode.Unauthorized, "请先登录");
        }

        // 支付单的归属与订单保持一致；强制覆盖请求体，避免用户指定他人 userId。
        request = request with { UserId = tenant.UserId };

        await using var lockHandle = await distributedLock.AcquireAsync(
            $"lock:payment:order:{request.BizNo}",
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(2),
            cancellationToken);
        if (lockHandle is null)
        {
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "支付处理中，请稍后重试");
        }

        var existing = await repository.GetByBizNoAsync(request.BizNo, cancellationToken);
        if (existing is not null)
        {
            return ApiResults.Ok(new { success = true, existing.Id, existing.PaymentNo, existing.Status });
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
            ? ApiResults.Ok(new { success = true, payment.Id, payment.PaymentNo })
            : ApiResults.Fail(BaseApiResponseCode.BadRequest, "支付单创建失败");
    }
}
