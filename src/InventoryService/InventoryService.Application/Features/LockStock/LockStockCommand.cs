using MediatR;

namespace InventoryService.Application.Features.LockStock;

public sealed record StockItem
{
    public long SkuId { get; init; }
    public int Quantity { get; init; }
}

public sealed record LockStockCommand : IRequest<object>
{
    public string BizNo { get; init; } = string.Empty;
    public List<StockItem> Items { get; init; } = [];
}
