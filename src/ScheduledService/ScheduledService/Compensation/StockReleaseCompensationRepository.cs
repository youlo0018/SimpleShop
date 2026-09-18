using FreeSql;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Domain.IRepository;
using Yitter.IdGenerator;

namespace ScheduledService.Compensation;

/// <summary>库存释放补偿仓储：扫描待重试记录、标记重试结果（与 ScheduledService 共用表）。</summary>
public sealed class StockReleaseCompensationRepository(
    [FromKeyedServices("scheduled")] IFreeSql freeSql) : IStockReleaseCompensationRepository
{
    /// <summary>更新：SaveAsync。</summary>
    public async Task SaveAsync(
        string orderNo,
        IReadOnlyCollection<OrderStockRequestItem> items,
        string error,
        CancellationToken cancellationToken)
    {
        foreach (var item in items)
        {
            var existing = await freeSql.Select<PendingStockRelease>()
                .Where(record => record.OrderNo == orderNo && record.SkuId == item.SkuId)
                .FirstAsync(cancellationToken);

            if (existing is not null)
            {
                existing.Quantity = item.Quantity;
                existing.LastError = error;
                await freeSql.Update<PendingStockRelease>().SetSource(existing).ExecuteAffrowsAsync(cancellationToken);
                continue;
            }

            await freeSql.Insert(new PendingStockRelease
                {
                    Id = YitIdHelper.NextId(),
                    OrderNo = orderNo,
                    SkuId = item.SkuId,
                    Quantity = item.Quantity,
                    LastError = error
                })
                .ExecuteAffrowsAsync(cancellationToken);
        }
    }

    /// <summary>查询：GetDueAsync。</summary>
    public Task<List<PendingStockRelease>> GetDueAsync(int limit, CancellationToken cancellationToken)
    {
        return freeSql.Select<PendingStockRelease>()
            .Where(record => record.NextRetryAt <= DateTime.Now)
            .OrderBy(record => record.NextRetryAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    /// <summary>标记补偿记录重试结果：成功删除/失败累计次数并延后下次重试。</summary>
    public async Task MarkRetriedAsync(PendingStockRelease record, string error, CancellationToken cancellationToken)
    {
        record.RetryCount++;
        record.LastError = error;
        record.NextRetryAt = DateTime.Now.AddSeconds(Math.Min(300, 30 * Math.Pow(2, Math.Min(record.RetryCount, 4))));
        await freeSql.Update<PendingStockRelease>().SetSource(record).ExecuteAffrowsAsync(cancellationToken);
    }

    /// <summary>删除：DeleteAsync。</summary>
    public Task DeleteAsync(string orderNo, IReadOnlyCollection<long> ids, CancellationToken cancellationToken)
    {
        return freeSql.Delete<PendingStockRelease>()
            .Where(record => record.OrderNo == orderNo && ids.Contains(record.Id))
            .ExecuteAffrowsAsync(cancellationToken);
    }
}
