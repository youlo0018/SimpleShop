using CommunalService.Domain.Contracts.Messages;
using MediatR;

namespace InventoryService.Application.Features.LockStock;

public sealed record StockItem
{
    /// <summary>商品 SKU ID。</summary>
    public long SkuId { get; init; }
    /// <summary>数量。</summary>
    public int Quantity { get; init; }
}

public sealed record LockStockCommand : IRequest<InventoryStockResponse>
{
    /// <summary>业务单号（订单号/退款号，幂等键）。</summary>
    public string BizNo { get; init; } = string.Empty;
    /// <summary>明细集合。</summary>
    public List<StockItem> Items { get; init; } = [];
}
