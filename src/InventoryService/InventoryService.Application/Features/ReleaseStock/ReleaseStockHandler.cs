using Locks = CommunalService.Domain.Infrastructure.Locks;
using CommunalService.Domain.Logging;
using CommunalService.Domain.Contracts.Messages;
using Microsoft.AspNetCore.Http;
using MediatR;
using InventoryService.Domain.IRepository;

namespace InventoryService.Application.Features.ReleaseStock;

/// <summary>
/// 释放库存（下单落库失败补偿 / 超时关单 / 退款回补共用）：锁定减少、可售回补；流水幂等保证重放安全。
/// </summary>
public sealed class ReleaseStockHandler(
    IStockRepository repository,
    Locks.IDistributedLock distributedLock,
    IHttpContextAccessor httpContextAccessor,
    IOperationLogger operationLogger)
    : IRequestHandler<ReleaseStockCommand, InventoryStockResponse>
{
    /// <summary>处理入口：释放库存（下单落库失败补偿 / 超时关单 / 退款回补共用）：锁定减少、可售回补；流水幂等保证重放安全。</summary>
    public async Task<InventoryStockResponse> Handle(ReleaseStockCommand request, CancellationToken cancellationToken)
    {
        foreach (var item in request.Items.OrderBy(item => item.SkuId))
        {
            await using var lockHandle = await distributedLock.AcquireAsync(
                $"lock:stock:{item.SkuId}", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(2), cancellationToken);

            if (lockHandle is null)
            {
                return new InventoryStockResponse { Success = false, Message = "库存繁忙，请稍后重试" };
            }

            if (!await repository.HasFlowAsync(request.BizNo, item.SkuId, "release", cancellationToken))
            {
                await repository.ReleaseAsync(item.SkuId, request.BizNo, item.Quantity, cancellationToken);
            }
        }

        // 释放代表取消占用；记录业务单号可以解释库存为何回到可售状态。
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            await operationLogger.LogAsync(
                httpContext,
                "release",
                "stock",
                request.BizNo,
                $"释放库存，SKU 数量：{request.Items.Count}，合计数量：{request.Items.Sum(item => item.Quantity)}",
                cancellationToken);
        }

        return new InventoryStockResponse { Success = true };
    }
}
