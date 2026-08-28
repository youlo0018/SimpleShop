using CommunalService.Domain.Infrastructure.Locks;
using CommunalService.Domain.Messaging;
using Microsoft.Extensions.Options;
using OrderService.Domain.Entity;
using OrderService.Domain.IRepository;
using OrderService.Infrastructure.ExternalServices;
using ScheduledService.Compensation;

namespace ScheduledService.Jobs;

/// <summary>
/// 订单支付超时关单任务。全局锁避免多实例重复扫描；订单锁保证关单、支付、取消互斥。
/// </summary>
public sealed class PaymentTimeoutCloseJob(
    IOrderRepository orderRepository,
    InventoryClient inventoryClient,
    IDistributedLock distributedLock,
    IStockReleaseCompensationRepository compensationRepository,
    IMessagePublisher messagePublisher,
    IOptions<PaymentTimeoutJobOptions> options,
    ILogger<PaymentTimeoutCloseJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        var interval = TimeSpan.FromSeconds(Math.Max(1, settings.IntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CloseExpiredOrdersWithGlobalLockAsync(settings, stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "支付超时关单任务执行失败。");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task CloseExpiredOrdersWithGlobalLockAsync(
        PaymentTimeoutJobOptions settings,
        CancellationToken cancellationToken)
    {
        var lockExpiry = TimeSpan.FromSeconds(Math.Max(settings.IntervalSeconds + 1, settings.ScanLockExpirySeconds));

        // 全局扫描锁是第一层保护；即使两个实例的调度时间完全一致，也只有一个实例读取同一批订单。
        await using var scanLockHandle = await distributedLock.AcquireAsync(
            "lock:job:payment-timeout-scan",
            lockExpiry,
            waitTimeout: TimeSpan.Zero,
            cancellationToken);

        if (scanLockHandle is null)
        {
            logger.LogDebug("其他定时实例正在执行支付超时扫描，本实例跳过本轮。");
            return;
        }

        // 先处理历史失败补偿，再扫描新超时订单，避免旧单被长期遗漏。
        await RetryPendingReleasesAsync(settings.BatchSize, cancellationToken);
        await CloseExpiredOrdersAsync(settings.BatchSize, cancellationToken);
    }

    private async Task CloseExpiredOrdersAsync(int batchSize, CancellationToken cancellationToken)
    {
        var orders = await orderRepository.QueryExpiredAwaitPaymentAsync(DateTime.Now, batchSize, cancellationToken);

        foreach (var expiredOrder in orders)
        {
            if (expiredOrder.OrderStatus != (int)OrderState.AwaitPayment || expiredOrder.IsPayment)
            {
                continue;
            }

            await using var orderLockHandle = await distributedLock.AcquireAsync(
                $"lock:order:{expiredOrder.Id}",
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(2),
                cancellationToken);

            if (orderLockHandle is null)
            {
                continue;
            }

            // 锁内重新读取状态，防止扫描结果已经过期。
            var latest = await orderRepository.GetByOrderNoAsync(expiredOrder.OrderNo, cancellationToken);
            if (latest is null || latest.OrderStatus != (int)OrderState.AwaitPayment || latest.IsPayment)
            {
                continue;
            }

            var closed = await orderRepository.TryCloseAsync(
                latest.Id,
                "支付超时自动关单",
                cancellationToken);
            if (!closed)
            {
                continue;
            }

            latest.OrderStatus = (int)OrderState.Closed;
            latest.CancelReason = "支付超时自动关单";

            var items = await orderRepository.GetItemsAsync(latest.Id, cancellationToken);
            if (items.Count > 0)
            {
                var stockItems = items
                    .GroupBy(item => item.SkuId)
                    .Select(item => new OrderStockRequestItem { SkuId = item.Key, Quantity = item.Sum(x => x.Quantity) })
                    .ToList();
                var released = await inventoryClient.ReleaseAsync(latest.OrderNo, stockItems, cancellationToken);
                if (!released)
                {
                    const string error = "库存服务返回失败";
                    await compensationRepository.SaveAsync(latest.OrderNo, stockItems, error, cancellationToken);
                    logger.LogError("支付超时关单后释放库存失败，已进入补偿队列。OrderNo：{OrderNo}。", latest.OrderNo);
                }
            }

            await messagePublisher.PublishAsync(
                "order.cancelled",
                latest.OrderNo,
                new MessageEnvelope<object>(
                    Guid.NewGuid(),
                    "order.cancelled",
                    DateTimeOffset.UtcNow,
                    Guid.NewGuid().ToString("N"),
                    latest.PlatformId,
                    0,
                    latest.CustomerId,
                    1,
                    new { orderId = latest.Id, orderNo = latest.OrderNo, reason = latest.CancelReason }),
                cancellationToken);
        }
    }

    private async Task RetryPendingReleasesAsync(int batchSize, CancellationToken cancellationToken)
    {
        var records = await compensationRepository.GetDueAsync(batchSize, cancellationToken);
        foreach (var group in records.GroupBy(record => record.OrderNo))
        {
            await using var orderLockHandle = await distributedLock.AcquireAsync(
                $"lock:order-release:{group.Key}",
                TimeSpan.FromSeconds(10),
                TimeSpan.Zero,
                cancellationToken);

            var stockItems = group
                .Select(record => new OrderStockRequestItem { SkuId = record.SkuId, Quantity = record.Quantity })
                .ToList();
            var released = await inventoryClient.ReleaseAsync(group.Key, stockItems, cancellationToken);
            if (released)
            {
                await compensationRepository.DeleteAsync(group.Key, group.Select(record => record.Id).ToList(), cancellationToken);
                logger.LogInformation("库存释放补偿成功。OrderNo：{OrderNo}。", group.Key);
                continue;
            }

            foreach (var record in group)
            {
                await compensationRepository.MarkRetriedAsync(record, "库存服务返回失败", cancellationToken);
            }
        }
    }
}
