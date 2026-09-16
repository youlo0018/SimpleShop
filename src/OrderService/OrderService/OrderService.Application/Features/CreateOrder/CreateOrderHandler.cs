using MediatR;
using CommunalService.Domain.Infrastructure.Locks;
using CommunalService.Domain.Logging;
using CommunalService.Domain.Messaging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Http;
using CommunalService.Domain.Contracts.Messages;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Entity;
using OrderService.Domain.IRepository;

namespace OrderService.Application.Features.CreateOrder;

public sealed class CreateOrderHandler(
    IOrderRepository repository,
    IDistributedLock distributedLock,
    IMemoryCache idempotencyCache,
    IMessagePublisher messagePublisher,
    IHttpContextAccessor httpContextAccessor,
    IOperationLogger operationLogger,
    ILogger<CreateOrderHandler> logger)
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

        var totalPrice = Math.Round(request.Items.Sum(item => item.Price * item.Quantity), 2, MidpointRounding.AwayFromZero);
        var orderNo = $"SO{DateTimeOffset.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(100000, 999999)}";

        // 营销预结算：服务端权威计算优惠并占用用户券；失败即拒绝下单（不能按原价偷偷成交）。
        var settle = await repository.SettleMarketingAsync(new MarketingSettleRequest
        {
            PlatformId = request.PlatformId,
            UserId = request.CustomerId,
            OrderNo = orderNo,
            SelectedUserCouponIds = request.SelectedUserCouponIds,
            Items = request.Items.Select(item => new MarketingSettleItem
            {
                SkuId = item.SkuId,
                PlatformId = item.PlatformId,
                MerchantId = item.MerchantId,
                ProductName = item.ProductName,
                Price = item.Price,
                Quantity = item.Quantity
            }).ToList()
        }, cancellationToken);
        if (settle is null || !settle.Success)
        {
            return new { success = false, message = settle?.Message ?? "优惠计算失败，请稍后重试" };
        }

        var marketingBySku = settle.Items.GroupBy(item => item.SkuId).ToDictionary(group => group.Key, group => group.First());
        var order = new Order
        {
            PlatformId = request.PlatformId,
            MerchantId = request.Items.FirstOrDefault()?.MerchantId ?? 0,
            OrderNo = orderNo,
            IdempotencyKey = request.IdempotencyKey,
            CustomerId = request.CustomerId,
            CustomerNo = request.CustomerNo,
            CustomerName = request.CustomerName,
            ReceiverName = request.ReceiverName,
            ReceiverPhone = request.ReceiverPhone,
            ReceiverAddress = request.ReceiverAddress,
            TotalPrice = totalPrice,
            AllDiscountPrice = settle.TotalDiscount,
            PaymentPrice = totalPrice - settle.TotalDiscount,
            CouponDiscountPrice = settle.CouponDiscount,
            ActivityDiscountPrice = settle.ActivityDiscount,
            PointDiscountPrice = 0,
            OrderStatus = (int)OrderState.AwaitPayment,
            PaymentExpiredAt = DateTimeOffset.UtcNow.AddMinutes(15).UtcDateTime
        };

        // 先把库存占住，再落订单；锁库存失败要把刚占用的券放回去。
        if (request.StockItems.Count > 0 && !await repository.LockStockAsync(order.OrderNo, MapStockItems(request.StockItems), cancellationToken))
        {
            await repository.ReleaseMarketingAsync(order.OrderNo, cancellationToken);
            return new { success = false, message = "库存不足或锁定失败" };
        }

        var orderItems = request.Items.Select(item =>
        {
            var hit = marketingBySku.GetValueOrDefault(item.SkuId);
            return new OrderItem
            {
                PlatformId = item.PlatformId,
                MerchantId = item.MerchantId,
                ProductId = item.SkuId,
                SkuId = item.SkuId,
                ProductName = item.ProductName,
                Price = item.Price,
                Quantity = item.Quantity,
                DiscountAmount = hit?.DiscountAmount ?? 0,
                MarketingType = hit?.HitType ?? 0,
                MarketingId = hit is null ? 0 : hit.HitType == 2 ? hit.UserCouponId : hit.ActivityId,
                MarketingName = hit is null ? string.Empty : hit.HitType == 2 ? hit.CouponName : hit.ActivityName,
                UserCouponId = hit?.UserCouponId ?? 0
            };
        }).ToList();

        var added = await repository.AddAsync(order, cancellationToken) &&
                    await repository.AddItemsAsync(order.Id, orderItems, cancellationToken);

        if (!added)
        {
            // 订单没建成，刚才占住的库存与券必须马上放回去。
            if (request.StockItems.Count > 0)
            {
                await repository.ReleaseStockAsync(order.OrderNo, MapStockItems(request.StockItems), cancellationToken);
            }
            await repository.ReleaseMarketingAsync(order.OrderNo, cancellationToken);

            var failed = new { success = false, message = "订单创建失败" };
            idempotencyCache.Set(idempotencyKey, failed, TimeSpan.FromMinutes(10));
            return failed;
        }

        // 订单落库后写营销参与/用券记录（失败只告警：交易已成，记录可后续对账补齐）。
        var committed = await repository.CommitMarketingAsync(new MarketingCommitRequest
        {
            OrderId = order.Id,
            OrderNo = order.OrderNo,
            UserId = order.CustomerId,
            PlatformId = order.PlatformId,
            Settle = settle
        }, cancellationToken);
        if (!committed)
        {
            logger.LogError("营销参与记录写入失败：OrderNo={OrderNo}", order.OrderNo);
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
            DiscountAmount = settle.TotalDiscount,
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
