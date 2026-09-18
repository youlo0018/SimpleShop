using InventoryService.Domain.Entity;

namespace InventoryService.Domain.IRepository;

public interface IStockRepository
{
    Task<Stock?> GetBySkuIdAsync(long skuId, CancellationToken cancellationToken = default);
    Task<bool> InitializeAsync(long skuId, long platformId, long merchantId, int quantity, CancellationToken cancellationToken = default);
    Task<bool> LockAsync(long skuId, string bizNo, int quantity, CancellationToken cancellationToken = default);
    Task<bool> DeductAsync(long skuId, string bizNo, int quantity, CancellationToken cancellationToken = default);
    Task<bool> ReleaseAsync(long skuId, string bizNo, int quantity, CancellationToken cancellationToken = default);
    Task<bool> RestoreAsync(long skuId, string bizNo, int quantity, CancellationToken cancellationToken = default);
    Task<bool> HasFlowAsync(string bizNo, long skuId, string action, CancellationToken cancellationToken = default);
}
