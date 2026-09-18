using CommunalService.Domain;
using InventoryService.Application.Features.DeductStock;
using InventoryService.Application.Features.LockStock;
using InventoryService.Application.Features.ReleaseStock;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InventoryService.Api.Controllers;

/// <summary>库存内部接口：供 OrderService 在锁定/扣减/释放链路调用，BizNo 为幂等键。</summary>
    public class StockController(IMediator mediator) : BaseController
{
    [HttpPost]
    /// <summary>锁定库存（POST）：下单时调用，失败表示库存不足。</summary>
    public async Task<ApiResponse> Lock([FromBody] LockStockCommand command)
        => Ok(await mediator.Send(command, CancellationToken.None));

    [HttpPost]
    /// <summary>扣减库存（POST）：支付成功后由事件消费者调用。</summary>
    public async Task<ApiResponse> Deduct([FromBody] DeductStockCommand command)
        => Ok(await mediator.Send(command, CancellationToken.None));

    [HttpPost]
    /// <summary>释放锁定（POST）：取消/关单时调用，幂等。</summary>
    public async Task<ApiResponse> Release([FromBody] ReleaseStockCommand command)
        => Ok(await mediator.Send(command, CancellationToken.None));
}
