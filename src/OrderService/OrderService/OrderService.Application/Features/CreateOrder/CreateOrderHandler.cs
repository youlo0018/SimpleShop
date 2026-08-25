using MediatR;
using CommunalService.Domain.Infrastructure.Locks;
using CommunalService.Domain.Logging;
using CommunalService.Domain.Messaging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Http;
using OrderService.Domain.Entity;
using OrderService.Domain.IRepository;

namespace OrderService.Application.Features.CreateOrder;

public sealed class CreateOrderHandler(
    IOrderRepository repository,
    IDistributedLock distributedLock,
    IMemoryCache idempotencyCache,
    IMessagePublisher messagePublisher,
    IHttpContextAccessor httpContextAccessor,
    IOperationLogger operationLogger)
    : IRequestHandler<CreateOrderCommand, object>
{
    public async Task<object> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            return new { success = false, message = "缺少幂等键" };
        }

        var idempotencyKey = $"idempotent:order:{request.IdempotencyKey}";
        if (idempotencyCache.TryGetValue(idempotencyKey, out object? cachedResult))
        {
            return cachedResult;
        }

        if (await repository.HasIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken))
        {
            var duplicate = new { success = false, message = "请勿重复提交订单" };
            idempotencyCache.Set(idempotencyKey, duplicate, TimeSpan.FromMinutes(10));
            return duplicate;
        }

        await using var userLock = await distributedLock.AcquireAsync(
            $"lock:order:create:{request.CustomerId}",
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(2),
            cancellationToken);

        if (userLock is null)
        {
            return new { success = false, message = "下单繁忙，请稍后重试" };
        }

        var totalPrice = request.Items.Sum(item => item.Price * item.Quantity);
        var order = new Order
        {
            PlatformId = request.PlatformId,
            OrderNo = $"SO{DateTimeOffset.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(100000, 999999)}",
            IdempotencyKey = request.IdempotencyKey,
            CustomerId = request.CustomerId,
            CustomerNo = request.CustomerNo,
            CustomerName = request.CustomerName,
            ReceiverName = request.ReceiverName,
            ReceiverPhone = request.ReceiverPhone,
            ReceiverAddress = request.ReceiverAddress,
            TotalPrice = totalPrice,
            AllDiscountPrice = 0,
            PaymentPrice = totalPrice,
            CouponDiscountPrice = 0,
            PointDiscountPrice = 0,
            OrderStatus = (int)OrderState.AwaitPayment,
            PaymentExpiredAt = DateTimeOffset.UtcNow.AddMinutes(15).UtcDateTime
        };

        // 先把库存占住，再落订单。
        if (request.StockItems.Count > 0 && !await repository.LockStockAsync(order.OrderNo, MapStockItems(request.StockItems), cancellationToken))
        {
            return new { success = false, message = "库存不足或锁定失败" };
        }

        var orderItems = request.Items.Select(item => new OrderItem
        {
            PlatformId = item.PlatformId,
            MerchantId = item.MerchantId,
            ProductId = item.SkuId,
            SkuId = item.SkuId,
            ProductName = item.ProductName,
            Price = item.Price,
            Quantity = item.Quantity
        }).ToList();

        var added = await repository.AddAsync(order, cancellationToken) &&
                    await repository.AddItemsAsync(order.Id, orderItems, cancellationToken);

        if (!added)
        {
            // 订单没建成，刚才占住的库存必须马上放回去。
            if (request.StockItems.Count > 0)
            {
                await repository.ReleaseStockAsync(order.OrderNo, MapStockItems(request.StockItems), cancellationToken);
            }

            var failed = new { success = false, message = "订单创建失败" };
            idempotencyCache.Set(idempotencyKey, failed, TimeSpan.FromMinutes(10));
            return failed;
        }

        await messagePublisher.PublishAsync(
            "order.created",
            order.OrderNo,
            new MessageEnvelope<object>(
                Guid.NewGuid(),
                "order.created",
                DateTimeOffset.UtcNow,
                Guid.NewGuid().ToString("N"),
                order.PlatformId,
                0,
                order.CustomerId,
                1,
                new
                {
                    orderId = order.Id,
                    orderNo = order.OrderNo,
                    amount = order.PaymentPrice
                }),
            cancellationToken);

        var result = new
        {
            success = true,
            order.Id,
            order.OrderNo,
            order.TotalPrice,
            order.PaymentPrice,
            order.PaymentExpiredAt
        };

        idempotencyCache.Set(idempotencyKey, result, TimeSpan.FromMinutes(10));

        // 订单创建属于关键审计动作；日志失败不影响已生成的订单。
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            await operationLogger.LogAsync(
                httpContext,
                "create",
                "order",
                order.OrderNo,
                $"创建订单，金额：{order.PaymentPrice:0.##}，SKU 数量：{request.Items.Count}",
                cancellationToken);
        }

        return result;
    }

    private static List<OrderStockRequestItem> MapStockItems(IReadOnlyCollection<OrderStockItem> items)
    {
        return items.Select(item => new OrderStockRequestItem { SkuId = item.SkuId, Quantity = item.Quantity }).ToList();
    }
}
