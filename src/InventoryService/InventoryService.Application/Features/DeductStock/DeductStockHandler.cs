using Locks = CommunalService.Domain.Infrastructure.Locks;
using MediatR;
using InventoryService.Domain.IRepository;

namespace InventoryService.Application.Features.DeductStock;

public sealed class DeductStockHandler(IStockRepository repository, Locks.IDistributedLock distributedLock)
    : IRequestHandler<DeductStockCommand, object>
{
    public async Task<object> Handle(DeductStockCommand request, CancellationToken cancellationToken)
    {
        foreach (var item in request.Items.OrderBy(item => item.SkuId))
        {
            await using var lockHandle = await distributedLock.AcquireAsync(
                $"lock:stock:{item.SkuId}", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(2), cancellationToken);

            if (lockHandle is null)
            {
                return new { success = false, message = "库存繁忙，请稍后重试" };
            }

            if (!await repository.DeductAsync(item.SkuId, request.BizNo, item.Quantity, cancellationToken))
            {
                return new { success = false, message = $"SKU {item.SkuId} 扣减失败" };
            }
        }

        return new { success = true };
    }
}
