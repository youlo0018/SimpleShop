using Locks = CommunalService.Domain.Infrastructure.Locks;
using CommunalService.Domain.Logging;
using CommunalService.Domain.Contracts.Messages;
using Microsoft.AspNetCore.Http;
using MediatR;
using InventoryService.Domain.IRepository;

namespace InventoryService.Application.Features.LockStock;

/// <summary>
/// 锁定库存（下单第一步）：按 SkuId 升序逐个加 SKU 锁（固定顺序防死锁）；
/// 流水按 BizNo+SKU+动作 幂等；任一 SKU 失败立即补偿释放已锁定的部分并返回失败。
/// </summary>
public sealed class LockStockHandler(
    IStockRepository repository,
    Locks.IDistributedLock distributedLock,
    IHttpContextAccessor httpContextAccessor,
    IOperationLogger operationLogger)
    : IRequestHandler<LockStockCommand, InventoryStockResponse>
{
    public async Task<InventoryStockResponse> Handle(LockStockCommand request, CancellationToken cancellationToken)
    {
        var lockedItems = new List<StockItem>();
        foreach (var item in request.Items.OrderBy(item => item.SkuId))
        {
            await using var lockHandle = await distributedLock.AcquireAsync(
                $"lock:stock:{item.SkuId}", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(2), cancellationToken);

            if (lockHandle is null)
            {
                return new InventoryStockResponse { Success = false, Message = "库存繁忙，请稍后重试" };
            }

            if (await repository.HasFlowAsync(request.BizNo, item.SkuId, "lock", cancellationToken))
            {
                continue;
            }

            if (!await repository.LockAsync(item.SkuId, request.BizNo, item.Quantity, cancellationToken) ||
                !await repository.HasFlowAsync(request.BizNo, item.SkuId, "lock", cancellationToken))
            {
                await CompensateAsync(lockedItems, request.BizNo, cancellationToken);
                return new InventoryStockResponse { Success = false, Message = $"SKU {item.SkuId} 库存不足" };
            }

            lockedItems.Add(item);
        }

        // 锁定代表库存承诺，审计中保留业务单号，方便后续对扣减/释放做链路串联。
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            await operationLogger.LogAsync(
                httpContext,
                "lock",
                "stock",
                request.BizNo,
                $"锁定库存，SKU 数量：{request.Items.Count}，合计数量：{request.Items.Sum(item => item.Quantity)}",
                cancellationToken);
        }

        return new InventoryStockResponse { Success = true };
    }

    private async Task CompensateAsync(
        IEnumerable<StockItem> lockedItems,
        string bizNo,
        CancellationToken cancellationToken)
    {
        foreach (var item in lockedItems)
        {
            await repository.ReleaseAsync(item.SkuId, $"{bizNo}:lock-compensate", item.Quantity, cancellationToken);
        }
    }
}
