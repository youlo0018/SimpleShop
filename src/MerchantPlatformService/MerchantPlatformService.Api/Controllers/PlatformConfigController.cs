using CommunalService.Domain;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MerchantPlatformService.Application.Features.PlatformConfigs.Get;
using MerchantPlatformService.Application.Features.PlatformConfigs.Save;

namespace MerchantPlatformService.Api.Controllers;

public class PlatformConfigController(IMediator mediator) : BaseController
{
    [HttpGet]
    public async Task<ApiResponse> Get([FromQuery] GetPlatformConfigQuery query)
        => Ok(await mediator.Send(query, CancellationToken.None));

    [HttpPost]
    public async Task<ApiResponse> Save([FromBody] SavePlatformConfigCommand command)
        => Ok(await mediator.Send(command, CancellationToken.None));
}
