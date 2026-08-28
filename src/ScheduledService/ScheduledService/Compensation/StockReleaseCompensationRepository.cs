using FreeSql;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Domain.IRepository;
using Yitter.IdGenerator;

namespace ScheduledService.Compensation;

public sealed class StockReleaseCompensationRepository(
    [FromKeyedServices("scheduled")] IFreeSql freeSql) : IStockReleaseCompensationRepository
{
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

    public Task<List<PendingStockRelease>> GetDueAsync(int limit, CancellationToken cancellationToken)
    {
        return freeSql.Select<PendingStockRelease>()
            .Where(record => record.NextRetryAt <= DateTime.Now)
            .OrderBy(record => record.NextRetryAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkRetriedAsync(PendingStockRelease record, string error, CancellationToken cancellationToken)
    {
        record.RetryCount++;
        record.LastError = error;
        record.NextRetryAt = DateTime.Now.AddSeconds(Math.Min(300, 30 * Math.Pow(2, Math.Min(record.RetryCount, 4))));
        await freeSql.Update<PendingStockRelease>().SetSource(record).ExecuteAffrowsAsync(cancellationToken);
    }

    public Task DeleteAsync(string orderNo, IReadOnlyCollection<long> ids, CancellationToken cancellationToken)
    {
        return freeSql.Delete<PendingStockRelease>()
            .Where(record => record.OrderNo == orderNo && ids.Contains(record.Id))
            .ExecuteAffrowsAsync(cancellationToken);
    }
}
