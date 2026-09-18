using FreeSql;
using InventoryService.Domain.Entity;
using InventoryService.Domain.IRepository;

namespace InventoryService.Infrastructure.Repository;

/// <summary>库存仓储实现：三列库存模型（Available 总量 / Locked 锁定 / Deducted 已扣），所有变更写流水并按 BizNo+SKU+动作 幂等。</summary>
    public class StockRepository(IFreeSql freeSql) : IStockRepository
{
    /// <summary>查询：GetBySkuIdAsync。</summary>
    /// <summary>按 SKU 取库存行。</summary>
    public Task<Stock?> GetBySkuIdAsync(long skuId, CancellationToken cancellationToken = default)
        => freeSql.Select<Stock>().Where(stock => stock.SkuId == skuId).FirstAsync();

    /// <summary>初始化 SKU 库存（商品创建事件消费，重复初始化幂等）。</summary>
    public async Task<bool> InitializeAsync(
        long skuId,
        long platformId,
        long merchantId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        if (quantity < 0 || await freeSql.Select<Stock>().AnyAsync(stock => stock.SkuId == skuId, cancellationToken))
        {
            return false;
        }

        var stock = new Stock { SkuId = skuId, PlatformId = platformId, MerchantId = merchantId, AvailableQuantity = quantity };
        return await freeSql.Insert(stock).ExecuteAffrowsAsync(cancellationToken) > 0;
    }

    /// <summary>库存操作（幂等键由调用方提供）：LockAsync。</summary>
    /// <summary>锁定库存：Locked + N；流水幂等键 BizNo+SKU+lock。</summary>
    public async Task<bool> LockAsync(long skuId, string bizNo, int quantity, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithFlowAsync(skuId, bizNo, "lock", quantity, cancellationToken, () => freeSql
            .Update<Stock>()
            .Where(stock => stock.SkuId == skuId &&
                            stock.AvailableQuantity - stock.LockedQuantity - stock.DeductedQuantity >= quantity)
            .Set(stock => stock.LockedQuantity == stock.LockedQuantity + quantity));
    }

    /// <summary>库存操作（幂等键由调用方提供）：DeductAsync。</summary>
    /// <summary>支付扣减：Locked - N、Deducted + N（Available 不变）；流水幂等键 BizNo+SKU+deduct。</summary>
    public async Task<bool> DeductAsync(long skuId, string bizNo, int quantity, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithFlowAsync(skuId, bizNo, "deduct", quantity, cancellationToken, () => freeSql
            .Update<Stock>()
            .Where(stock => stock.SkuId == skuId && stock.LockedQuantity >= quantity)
            .Set(stock => stock.LockedQuantity == stock.LockedQuantity - quantity)
            .Set(stock => stock.DeductedQuantity == stock.DeductedQuantity + quantity));
    }

    /// <summary>库存操作（幂等键由调用方提供）：ReleaseAsync。</summary>
    /// <summary>释放锁定：Locked - N（取消/关单）；流水幂等键 BizNo+SKU+release。</summary>
    public async Task<bool> ReleaseAsync(long skuId, string bizNo, int quantity, CancellationToken cancellationToken = default)
    {
        // 超时关单可能已经把锁定数量清零；释放接口必须兼容“无账可释放”的幂等场景。
        if (!await freeSql.Select<Stock>().AnyAsync(
                stock => stock.SkuId == skuId && stock.LockedQuantity >= quantity,
                cancellationToken))
        {
            return true;
        }

        return await ExecuteWithFlowAsync(skuId, bizNo, "release", quantity, cancellationToken, () => freeSql
            .Update<Stock>()
            .Where(stock => stock.SkuId == skuId && stock.LockedQuantity >= quantity)
            .Set(stock => stock.LockedQuantity == stock.LockedQuantity - quantity));
    }

    /// <summary>库存操作（幂等键由调用方提供）：RestoreAsync。</summary>
    /// <summary>退款回补：Deducted - N（可用回增）；流水幂等键 BizNo+SKU+restore。</summary>
    public async Task<bool> RestoreAsync(long skuId, string bizNo, int quantity, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithFlowAsync(skuId, bizNo, "restore", quantity, cancellationToken, () => freeSql
            .Update<Stock>()
            .Where(stock => stock.SkuId == skuId && stock.DeductedQuantity >= quantity)
            .Set(stock => stock.DeductedQuantity == stock.DeductedQuantity - quantity));
    }

    /// <summary>流水是否已存在（幂等去重）。</summary>
    public Task<bool> HasFlowAsync(string bizNo, long skuId, string action, CancellationToken cancellationToken = default)
        => freeSql.Select<StockFlow>()
            .AnyAsync(flow => flow.BizNo == bizNo && flow.SkuId == skuId && flow.Action == action);

    /// <summary>库存变更与流水的统一执行器：条件更新 + 流水插入，任一步失败回滚事务。</summary>
    private async Task<bool> ExecuteWithFlowAsync(
        long skuId,
        string bizNo,
        string action,
        int quantity,
        CancellationToken cancellationToken,
        Func<IUpdate<Stock>> update)
    {
        if (quantity <= 0)
        {
            return false;
        }

        // 库存数量和流水必须在同一事务里成功，否则幂等检查会与实际账务脱节。
        var success = false;
        freeSql.Transaction(() =>
        {
            // 条件不满足是正常的售罄/并发竞争，必须回给上游 false，而不是把业务失败变成 gRPC 异常。
            if (update().ExecuteAffrows() <= 0) return;

            var flow = new StockFlow { SkuId = skuId, BizNo = bizNo, Action = action, Quantity = quantity };
            success = freeSql.Insert(flow).ExecuteAffrows() > 0;
        });

        return success;
    }
}
