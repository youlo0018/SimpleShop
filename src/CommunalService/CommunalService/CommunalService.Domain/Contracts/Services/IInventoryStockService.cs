using CommunalService.Domain.Contracts.Messages;
using MagicOnion;

namespace CommunalService.Domain.Contracts.Services;

/// <summary>
/// 服务间库存操作契约；订单和定时关单必须通过该接口调用，不直接访问库存库或 REST 内网接口。
/// </summary>
public interface IInventoryStockService : IService<IInventoryStockService>
{
    UnaryResult<InventoryStockResponse> LockAsync(InventoryStockRequest request);
    UnaryResult<InventoryStockResponse> ReleaseAsync(InventoryStockRequest request);
    UnaryResult<InventoryStockResponse> DeductAsync(InventoryStockRequest request);
}
