using System.Text;
using System.Text.Json;
using CommunalService.Domain.Infrastructure.Locks;
using CommunalService.Domain.Messaging;
using Microsoft.Extensions.Configuration;
using OrderService.Domain.Entity;
using OrderService.Domain.IRepository;
using OrderService.Infrastructure.ExternalServices;

namespace OrderService.Api.BackgroundJobs;

/// <summary>
/// 订单的“闹钟”：用户一直不付款时，把过期待支付订单关闭并释放库存。
/// 分布式锁保证多个实例同时扫描也不会重复关单。
/// </summary>
public sealed class PaymentTimeoutCloseJob(
    IOrderRepository orderRepository,
    InventoryClient inventoryClient,
    IDistributedLock distributedLock,
    IMessagePublisher messagePublisher,
    IConfiguration configuration,
    ILogger<PaymentTimeoutCloseJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var batchSize = configuration.GetValue<int?>("Orders:TimeoutBatchSize") ?? 50;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CloseExpiredOrdersAsync(batchSize, stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "支付超时关单任务执行失败。");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task CloseExpiredOrdersAsync(int batchSize, CancellationToken cancellationToken)
    {
        var orders = await orderRepository.QueryExpiredAwaitPaymentAsync(DateTime.Now, batchSize, cancellationToken);
        foreach (var order in orders)
        {
            if (order.OrderStatus != (int)OrderState.AwaitPayment || order.IsPayment)
            {
                continue;
            }

            await using var lockHandle = await distributedLock.AcquireAsync(
                $"lock:order:close:{order.Id}",
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(2),
                cancellationToken);

            if (lockHandle is null)
            {
                continue;
            }

            // 拿到锁后再查一次，避免刚好和支付成功回调撞车。
            var latest = await orderRepository.GetByOrderNoAsync(order.OrderNo, cancellationToken);
            if (latest is null || latest.OrderStatus != (int)OrderState.AwaitPayment || latest.IsPayment)
            {
                continue;
            }

            latest.OrderStatus = (int)OrderState.Closed;
            latest.CancelReason = "支付超时自动关单";
            latest.UpdatedAt = DateTime.Now;
            if (!await orderRepository.UpdateAsync(latest, cancellationToken))
            {
                continue;
            }

            var items = await orderRepository.GetItemsAsync(latest.Id, cancellationToken);
            if (items.Count > 0)
            {
                var stockItems = items
                    .GroupBy(item => item.SkuId)
                    .Select(item => new OrderStockRequestItem { SkuId = item.Key, Quantity = item.Sum(x => x.Quantity) })
                    .ToList();
                await inventoryClient.ReleaseAsync(latest.OrderNo, stockItems, cancellationToken);
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
}
