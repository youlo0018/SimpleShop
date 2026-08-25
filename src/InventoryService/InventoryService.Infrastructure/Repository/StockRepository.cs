using FreeSql;
using InventoryService.Domain.Entity;
using InventoryService.Domain.IRepository;

namespace InventoryService.Infrastructure.Repository;

public class StockRepository(IFreeSql freeSql) : IStockRepository
{
    public Task<Stock?> GetBySkuIdAsync(long skuId, CancellationToken cancellationToken = default)
        => freeSql.Select<Stock>().Where(stock => stock.SkuId == skuId).FirstAsync();

    public async Task<bool> LockAsync(long skuId, string bizNo, int quantity, CancellationToken cancellationToken = default)
    {
        var updated = await freeSql.Update<Stock>()
            .Where(stock => stock.SkuId == skuId && stock.AvailableQuantity - stock.LockedQuantity >= quantity)
            .Set(stock => stock.LockedQuantity, quantity)
            .ExecuteAffrowsAsync(cancellationToken);

        return await AddFlowAndReturnAsync(updated, skuId, bizNo, "lock", quantity, cancellationToken);
    }

    public async Task<bool> DeductAsync(long skuId, string bizNo, int quantity, CancellationToken cancellationToken = default)
    {
        var updated = await freeSql.Update<Stock>()
            .Where(stock => stock.SkuId == skuId && stock.LockedQuantity >= quantity)
            .Set(stock => stock.LockedQuantity, -quantity)
            .Set(stock => stock.DeductedQuantity, quantity)
            .ExecuteAffrowsAsync(cancellationToken);

        return await AddFlowAndReturnAsync(updated, skuId, bizNo, "deduct", quantity, cancellationToken);
    }

    public async Task<bool> ReleaseAsync(long skuId, string bizNo, int quantity, CancellationToken cancellationToken = default)
    {
        var updated = await freeSql.Update<Stock>()
            .Where(stock => stock.SkuId == skuId && stock.LockedQuantity >= quantity)
            .Set(stock => stock.LockedQuantity, -quantity)
            .ExecuteAffrowsAsync(cancellationToken);

        return await AddFlowAndReturnAsync(updated, skuId, bizNo, "release", quantity, cancellationToken);
    }

    public Task<bool> HasFlowAsync(string bizNo, long skuId, string action, CancellationToken cancellationToken = default)
        => freeSql.Select<StockFlow>()
            .AnyAsync(flow => flow.BizNo == bizNo && flow.SkuId == skuId && flow.Action == action);

    private async Task<bool> AddFlowAndReturnAsync(
        long affected,
        long skuId,
        string bizNo,
        string action,
        int quantity,
        CancellationToken cancellationToken)
    {
        if (affected <= 0)
        {
            return false;
        }

        var flow = new StockFlow { SkuId = skuId, BizNo = bizNo, Action = action, Quantity = quantity };
        return await freeSql.Insert(flow).ExecuteAffrowsAsync(cancellationToken) > 0;
    }
}
