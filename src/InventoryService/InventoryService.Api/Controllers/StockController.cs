using CommunalService.Domain;
using InventoryService.Application.Features.DeductStock;
using InventoryService.Application.Features.LockStock;
using InventoryService.Application.Features.ReleaseStock;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InventoryService.Api.Controllers;

public class StockController(IMediator mediator) : BaseController
{
    [HttpPost]
    public async Task<ApiResponse> Lock([FromBody] LockStockCommand command)
        => Ok(await mediator.Send(command, CancellationToken.None));

    [HttpPost]
    public async Task<ApiResponse> Deduct([FromBody] DeductStockCommand command)
        => Ok(await mediator.Send(command, CancellationToken.None));

    [HttpPost]
    public async Task<ApiResponse> Release([FromBody] ReleaseStockCommand command)
        => Ok(await mediator.Send(command, CancellationToken.None));
}
