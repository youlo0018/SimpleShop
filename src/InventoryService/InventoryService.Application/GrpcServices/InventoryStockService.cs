using CommunalService.Domain.Contracts.Messages;
using CommunalService.Domain.Contracts.Services;
using InventoryService.Application.Features.DeductStock;
using InventoryService.Application.Features.LockStock;
using InventoryService.Application.Features.ReleaseStock;
using MagicOnion;
using MagicOnion.Server;
using MediatR;

namespace InventoryService.Application.GrpcServices;

/// <summary>
/// 库存内网 gRPC 服务：把 REST 留给外部排障和网关，订单侧同步调用走共享契约。
/// </summary>
public sealed class InventoryStockService(IMediator mediator) : ServiceBase<IInventoryStockService>, IInventoryStockService
{
    /// <summary>库存操作（幂等键由调用方提供）：LockAsync。</summary>
    public async UnaryResult<InventoryStockResponse> LockAsync(InventoryStockRequest request)
        => await mediator.Send(ToLockCommand(request), CancellationToken.None);

    /// <summary>库存操作（幂等键由调用方提供）：ReleaseAsync。</summary>
    public async UnaryResult<InventoryStockResponse> ReleaseAsync(InventoryStockRequest request)
        => await mediator.Send(ToReleaseCommand(request), CancellationToken.None);

    /// <summary>库存操作（幂等键由调用方提供）：DeductAsync。</summary>
    public async UnaryResult<InventoryStockResponse> DeductAsync(InventoryStockRequest request)
        => await mediator.Send(ToDeductCommand(request), CancellationToken.None);

    /// <summary>内部处理：ToLockCommand。</summary>
    private static LockStockCommand ToLockCommand(InventoryStockRequest request) => new()
    {
        BizNo = request.BizNo,
        Items = ToItems(request)
    };

    /// <summary>内部处理：ToReleaseCommand。</summary>
    private static ReleaseStockCommand ToReleaseCommand(InventoryStockRequest request) => new()
    {
        BizNo = request.BizNo,
        Items = ToItems(request)
    };

    /// <summary>内部处理：ToDeductCommand。</summary>
    private static DeductStockCommand ToDeductCommand(InventoryStockRequest request) => new()
    {
        BizNo = request.BizNo,
        Items = ToItems(request)
    };

    /// <summary>内部处理：ToItems。</summary>
    private static List<StockItem> ToItems(InventoryStockRequest request)
        => request.Items.Select(item => new StockItem { SkuId = item.SkuId, Quantity = item.Quantity }).ToList();
}
