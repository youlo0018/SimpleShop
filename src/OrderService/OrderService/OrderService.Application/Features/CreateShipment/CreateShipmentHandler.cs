using CommunalService.Domain;
using CommunalService.Domain.Enums;
using CommunalService.Domain.Infrastructure.Locks;
using MediatR;
using OrderService.Domain.Entity;
using OrderService.Domain.IRepository;

namespace OrderService.Application.Features.CreateShipment;

/// <summary>
/// 商户发货：只有已支付订单能创建发货单；订单锁保证发货和取消/退款不会同时改状态。
/// </summary>
public sealed class CreateShipmentHandler(
    IOrderRepository orderRepository,
    IShipmentRepository shipmentRepository,
    TenantContext tenant,
    IDistributedLock distributedLock)
    : IRequestHandler<CreateShipmentCommand, ApiResponse>
{
    /// <summary>处理入口：商户发货：只有已支付订单能创建发货单；订单锁保证发货和取消/退款不会同时改状态。</summary>
    public async Task<ApiResponse> Handle(CreateShipmentCommand request, CancellationToken cancellationToken)
    {
        if (request.Items.Count == 0)
        {
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "发货明细不能为空");
        }

        // 先做无锁预检，减少无效锁竞争；锁内还会重新读取订单状态。
        var existing = await orderRepository.QueryByIdAsync(request.OrderId);
        if (existing is null)
        {
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "订单不存在");
        }

        if (existing.OrderStatus != (int)OrderState.Paid)
        {
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "仅已支付订单可发货");
        }

        await using var lockHandle = await distributedLock.AcquireAsync(
            $"lock:order:{request.OrderId}",
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(2),
            cancellationToken);

        if (lockHandle is null)
        {
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "订单处理中，请稍后重试");
        }

        var order = await orderRepository.QueryByIdAsync(request.OrderId);
        if (order is null || order.OrderStatus != (int)OrderState.Paid)
        {
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "当前订单状态不可发货");
        }

        // 租户归属：平台限本平台，商户限本商户；命令里的归属以订单事实为准回填。
        if (tenant.IsPlatform && order.PlatformId != tenant.PlatformId)
        {
            return ApiResults.Fail(BaseApiResponseCode.Forbidden, "无权操作该订单");
        }
        if (tenant.IsMerchant && order.MerchantId != tenant.MerchantId)
        {
            return ApiResults.Fail(BaseApiResponseCode.Forbidden, "无权操作该订单");
        }
        request = request with
        {
            PlatformId = order.PlatformId,
            MerchantId = request.MerchantId > 0 ? request.MerchantId : order.MerchantId
        };

        var items = request.Items.Select(item => new ShipmentItem
        {
            OrderItemId = item.OrderItemId,
            SkuId = item.SkuId,
            Quantity = item.Quantity,
            Remark = string.Empty
        }).ToList();

        var shipment = new Shipment
        {
            PlatformId = order.PlatformId,
            MerchantId = request.MerchantId,
            OrderId = order.Id,
            ShipmentNo = $"SH{DateTimeOffset.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}",
            LogisticsCompany = request.LogisticsCompany,
            TrackingNo = request.TrackingNo,
            Status = 20,
            ShippedAt = DateTime.Now
        };

        var added = await shipmentRepository.AddAsync(shipment, items, cancellationToken);
        if (!added)
        {
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "发货单创建失败");
        }

        var shipped = await orderRepository.TryMarkShippedAsync(order.Id, cancellationToken);
        if (!shipped)
        {
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "发货单已创建，但订单状态更新失败");
        }

        return ApiResults.Ok(new { success = true, shipmentNo = shipment.ShipmentNo });
    }
}
