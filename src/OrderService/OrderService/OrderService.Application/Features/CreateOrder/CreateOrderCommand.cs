using MediatR;

namespace OrderService.Application.Features.CreateOrder;

public sealed record CreateOrderItem
{
    /// <summary>商品 SKU ID。</summary>
    public long SkuId { get; init; }
    /// <summary>平台 ID。</summary>
    public long PlatformId { get; init; }
    /// <summary>商户 ID。</summary>
    public long MerchantId { get; init; }
    /// <summary>商品名称快照。</summary>
    public string ProductName { get; init; } = string.Empty;
    /// <summary>单价（元）。</summary>
    public decimal Price { get; init; }
    /// <summary>数量。</summary>
    public int Quantity { get; init; }
}

public sealed record CreateOrderCommand : IRequest<object>
{
    /// <summary>幂等键（重复提交返回同一结果）。</summary>
    public string IdempotencyKey { get; init; } = string.Empty;
    /// <summary>平台 ID。</summary>
    public long PlatformId { get; init; }
    /// <summary>客户 ID。</summary>
    public long CustomerId { get; init; }
    /// <summary>客户编号（对账/展示用）。</summary>
    public string CustomerNo { get; init; } = string.Empty;
    /// <summary>客户登录名（平台内唯一）。</summary>
    public string CustomerName { get; init; } = string.Empty;
    /// <summary>收货人。</summary>
    public string ReceiverName { get; init; } = string.Empty;
    /// <summary>收货电话。</summary>
    public string ReceiverPhone { get; init; } = string.Empty;
    /// <summary>收货地址快照。</summary>
    public string ReceiverAddress { get; init; } = string.Empty;
    /// <summary>明细集合。</summary>
    public List<CreateOrderItem> Items { get; init; } = [];
    /// <summary>库存明细（锁定/扣减/释放的 SKU 与数量）。</summary>
    public List<OrderStockItem> StockItems { get; init; } = [];

    /// <summary>提交页用户勾选使用的用户券；null=自动全部可用券，[]=不使用券。</summary>
    public List<long>? SelectedUserCouponIds { get; init; }
}

public sealed record OrderStockItem
{
    /// <summary>商品 SKU ID。</summary>
    public long SkuId { get; init; }
    /// <summary>数量。</summary>
    public int Quantity { get; init; }
}
