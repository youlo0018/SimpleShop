using MediatR;
using InventoryService.Application.Features.LockStock;

namespace InventoryService.Application.Features.ReleaseStock;

public sealed record ReleaseStockCommand : IRequest<object>
{
    public string BizNo { get; init; } = string.Empty;
    public List<StockItem> Items { get; init; } = [];
}
