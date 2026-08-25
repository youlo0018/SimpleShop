using Locks = CommunalService.Domain.Infrastructure.Locks;
using MediatR;
using InventoryService.Domain.IRepository;

namespace InventoryService.Application.Features.LockStock;

public sealed class LockStockHandler(IStockRepository repository, Locks.IDistributedLock distributedLock)
    : IRequestHandler<LockStockCommand, object>
{
    public async Task<object> Handle(LockStockCommand request, CancellationToken cancellationToken)
    {
        foreach (var item in request.Items.OrderBy(item => item.SkuId))
        {
            await using var lockHandle = await distributedLock.AcquireAsync(
                $"lock:stock:{item.SkuId}", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(2), cancellationToken);

            if (lockHandle is null)
            {
                return new { success = false, message = "库存繁忙，请稍后重试" };
            }

            if (!await repository.LockAsync(item.SkuId, request.BizNo, item.Quantity, cancellationToken))
            {
                return new { success = false, message = $"SKU {item.SkuId} 库存不足" };
            }
        }

        return new { success = true };
    }
}
