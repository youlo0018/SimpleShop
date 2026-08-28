using OrderService.Domain.IRepository;

namespace ScheduledService.Compensation;

public interface IStockReleaseCompensationRepository
{
    Task SaveAsync(string orderNo, IReadOnlyCollection<OrderStockRequestItem> items, string error, CancellationToken cancellationToken);

    Task<List<PendingStockRelease>> GetDueAsync(int limit, CancellationToken cancellationToken);

    Task MarkRetriedAsync(PendingStockRelease record, string error, CancellationToken cancellationToken);

    Task DeleteAsync(string orderNo, IReadOnlyCollection<long> ids, CancellationToken cancellationToken);
}
