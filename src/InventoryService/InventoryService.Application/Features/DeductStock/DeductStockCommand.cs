using CommunalService.Domain.Contracts.Messages;
using MediatR;
using InventoryService.Application.Features.LockStock;

namespace InventoryService.Application.Features.DeductStock;

public sealed record DeductStockCommand : IRequest<InventoryStockResponse>
{
    public string BizNo { get; init; } = string.Empty;
    public List<StockItem> Items { get; init; } = [];
}
