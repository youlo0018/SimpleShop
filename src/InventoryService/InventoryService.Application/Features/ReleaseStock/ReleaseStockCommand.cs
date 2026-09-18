using CommunalService.Domain.Contracts.Messages;
using MediatR;
using InventoryService.Application.Features.LockStock;

namespace InventoryService.Application.Features.ReleaseStock;

public sealed record ReleaseStockCommand : IRequest<InventoryStockResponse>
{
    /// <summary>业务单号（订单号/退款号，幂等键）。</summary>
    public string BizNo { get; init; } = string.Empty;
    /// <summary>明细集合。</summary>
    public List<StockItem> Items { get; init; } = [];
}
