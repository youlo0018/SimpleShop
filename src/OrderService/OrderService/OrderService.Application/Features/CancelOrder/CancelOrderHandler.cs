using CommunalService.Domain;
using CommunalService.Domain.Enums;
using CommunalService.Domain.Infrastructure.Locks;
using MediatR;
using OrderService.Domain.Entity;
using OrderService.Domain.IRepository;

namespace OrderService.Application.Features.CancelOrder;

public sealed class CancelOrderHandler(
    IOrderRepository repository,
    TenantContext tenant,
    IDistributedLock distributedLock)
    : IRequestHandler<CancelOrderCommand, ApiResponse>
{
    public async Task<ApiResponse> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await repository.QueryByIdAsync(request.Id);
        if (order is null || (!request.OverrideCustomerScope && order.CustomerId != request.CustomerId))
        {
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "订单不存在");
        }

        // 后台租户（平台/商户）取消时校验归属范围。
        if (tenant.IsPlatform && order.PlatformId != tenant.PlatformId)
        {
            return ApiResults.Fail(BaseApiResponseCode.Forbidden, "无权操作该订单");
        }
        if (tenant.IsMerchant && order.MerchantId != tenant.MerchantId)
        {
            return ApiResults.Fail(BaseApiResponseCode.Forbidden, "无权操作该订单");
        }

        if (!order.CanCancel)
        {
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "当前订单状态不可取消");
        }

        await using var lockHandle = await distributedLock.AcquireAsync(
            $"lock:order:{order.Id}",
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(2),
            cancellationToken);

        if (lockHandle is null)
        {
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "订单处理中，请稍后重试");
        }

        // 条件更新是数据库层兜底：即使锁过期，也不会把已支付或已关闭订单改回取消。
        var cancelled = await repository.TryCancelAsync(
            order.Id,
            request.CustomerId,
            request.Reason,
            requireCustomerId: !request.OverrideCustomerScope,
            cancellationToken);

        return cancelled
            ? ApiResults.Ok(new { success = true, orderId = order.Id, status = (int)OrderState.Cancelled })
            : ApiResults.Fail(BaseApiResponseCode.BadRequest, "当前订单状态不可取消");
    }
}
