using CommunalService.Domain;
using CommunalService.Domain.Enums;
using CommunalService.Domain.Infrastructure.Locks;
using CommunalService.Domain.Messaging;
using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Entity;
using OrderService.Domain.IRepository;

namespace OrderService.Application.Features.CancelOrder;

/// <summary>取消订单：归属/状态校验 → 条件置 90 → 释放库存（失败写补偿表）→ 发 order.cancelled 回退券。</summary>
public sealed class CancelOrderHandler(
    IOrderRepository repository,
    TenantContext tenant,
    IDistributedLock distributedLock,
    IMessagePublisher messagePublisher,
    ILogger<CancelOrderHandler> logger)
    : IRequestHandler<CancelOrderCommand, ApiResponse>
{
    /// <summary>处理入口：取消订单：归属/状态校验 → 条件置 90 → 释放库存（失败写补偿表）→ 发 order.cancelled 回退券。</summary>
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

        if (!cancelled)
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "当前订单状态不可取消");

        // 未支付订单取消后必须释放下单时锁定的库存；释放失败写补偿表由定时任务重试。
        if (order.StockLocked)
        {
            var items = await repository.GetItemsAsync(order.Id, cancellationToken);
            var stockItems = items
                .GroupBy(item => item.SkuId)
                .Select(group => new OrderStockRequestItem { SkuId = group.Key, Quantity = group.Sum(item => item.Quantity) })
                .ToList();
            var released = stockItems.Count == 0 || await repository.ReleaseStockAsync(order.OrderNo, stockItems, cancellationToken);
            if (!released)
            {
                await repository.SavePendingStockReleaseAsync(order.OrderNo, stockItems, "取消订单释放库存失败", cancellationToken);
                logger.LogError("取消订单释放库存失败，已进入补偿队列。OrderNo：{OrderNo}", order.OrderNo);
            }
        }

        // 发布取消事件：营销服务据此回退券占用（营销侧幂等，重复投递无副作用）。
        await messagePublisher.PublishAsync(
            "order.cancelled",
            order.OrderNo,
            new MessageEnvelope<object>(
                Guid.NewGuid(),
                "order.cancelled",
                DateTimeOffset.UtcNow,
                Guid.NewGuid().ToString("N"),
                order.PlatformId,
                order.MerchantId,
                order.CustomerId,
                1,
                new { orderId = order.Id, orderNo = order.OrderNo, reason = request.Reason }),
            cancellationToken);

        return ApiResults.Ok(new { success = true, orderId = order.Id, status = (int)OrderState.Cancelled });
    }
}
