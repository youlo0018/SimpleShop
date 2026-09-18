using Locks = CommunalService.Domain.Infrastructure.Locks;
using CommunalService.Domain.Logging;
using CommunalService.Domain.Contracts.Messages;
using Microsoft.AspNetCore.Http;
using MediatR;
using InventoryService.Domain.IRepository;

namespace InventoryService.Application.Features.DeductStock;

/// <summary>
/// 扣减库存（支付成功事件驱动）：锁定数量转为已扣数量；流水幂等防重复投递；失败回补已扣部分。
/// </summary>
public sealed class DeductStockHandler(
    IStockRepository repository,
    Locks.IDistributedLock distributedLock,
    IHttpContextAccessor httpContextAccessor,
    IOperationLogger operationLogger)
    : IRequestHandler<DeductStockCommand, InventoryStockResponse>
{
    /// <summary>处理入口：扣减库存（支付成功事件驱动）：锁定数量转为已扣数量；流水幂等防重复投递；失败回补已扣部分。</summary>
    public async Task<InventoryStockResponse> Handle(DeductStockCommand request, CancellationToken cancellationToken)
    {
        var deductedItems = new List<LockStock.StockItem>();
        foreach (var item in request.Items.OrderBy(item => item.SkuId))
        {
            await using var lockHandle = await distributedLock.AcquireAsync(
                $"lock:stock:{item.SkuId}", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(2), cancellationToken);

            if (lockHandle is null)
            {
                return new InventoryStockResponse { Success = false, Message = "库存繁忙，请稍后重试" };
            }

            if (await repository.HasFlowAsync(request.BizNo, item.SkuId, "deduct", cancellationToken))
            {
                continue;
            }

            if (!await repository.DeductAsync(item.SkuId, request.BizNo, item.Quantity, cancellationToken))
            {
                foreach (var deducted in deductedItems)
                {
                    await repository.RestoreAsync(deducted.SkuId, $"{request.BizNo}:deduct-compensate", deducted.Quantity, cancellationToken);
                }

                return new InventoryStockResponse { Success = false, Message = $"SKU {item.SkuId} 扣减失败" };
            }

            deductedItems.Add(item);
        }

        // 扣减是库存事实变更；即使由后台事件触发，也要保留业务单号用于审计追踪。
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            await operationLogger.LogAsync(
                httpContext,
                "deduct",
                "stock",
                request.BizNo,
                $"扣减库存，SKU 数量：{request.Items.Count}，合计数量：{request.Items.Sum(item => item.Quantity)}",
                cancellationToken);
        }

        return new InventoryStockResponse { Success = true };
    }
}
