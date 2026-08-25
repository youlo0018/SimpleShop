using CommunalService.Domain;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MerchantPlatformService.Application.Features.Merchants.Create;
using MerchantPlatformService.Application.Features.Merchants.Get;
using MerchantPlatformService.Application.Features.Merchants.Review;

namespace MerchantPlatformService.Api.Controllers;

public class MerchantController(IMediator mediator) : BaseController
{
    [HttpPost]
    public async Task<ApiResponse> Create([FromBody] CreateMerchantCommand command)
        => Ok(await mediator.Send(command, CancellationToken.None));

    [HttpGet]
    public async Task<ApiResponse> Get([FromQuery] GetMerchantQuery query)
        => Ok(await mediator.Send(query, CancellationToken.None));

    [HttpPost]
    public async Task<ApiResponse> Review([FromBody] ReviewMerchantCommand command)
        => Ok(await mediator.Send(command, CancellationToken.None));
}
