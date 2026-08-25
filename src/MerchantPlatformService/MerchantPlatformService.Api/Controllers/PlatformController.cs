using CommunalService.Domain;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MerchantPlatformService.Application.Features.Platforms.Create;
using MerchantPlatformService.Application.Features.Platforms.Get;

namespace MerchantPlatformService.Api.Controllers;

public class PlatformController(IMediator mediator) : BaseController
{
    [HttpPost]
    public async Task<ApiResponse> Create([FromBody] CreatePlatformCommand command)
        => Ok(await mediator.Send(command, CancellationToken.None));

    [HttpGet]
    public async Task<ApiResponse> Get([FromQuery] GetPlatformQuery query)
        => Ok(await mediator.Send(query, CancellationToken.None));
}
