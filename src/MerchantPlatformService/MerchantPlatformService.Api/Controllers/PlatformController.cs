using CommunalService.Domain;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MerchantPlatformService.Application.Features.Platforms.Create;
using MerchantPlatformService.Application.Features.Platforms.Edit;
using MerchantPlatformService.Application.Features.Platforms.Get;
using MerchantPlatformService.Application.Features.Platforms.List;
using MerchantPlatformService.Application.Features.Platforms.SetEnabled;

namespace MerchantPlatformService.Api.Controllers;

/// <summary>平台入口：控制器只做协议转换；列表过滤、编辑校验在 Application 层。</summary>
public class PlatformController(IMediator mediator) : BaseController
{
    [HttpGet]
    public Task<ApiResponse> List([FromQuery] ListPlatformsQuery query)
        => mediator.Send(query, CancellationToken.None);

    [HttpPost]
    public async Task<ApiResponse> Create([FromBody] CreatePlatformCommand command)
        => Ok(await mediator.Send(command, CancellationToken.None));

    [HttpPut("{id}")]
    public Task<ApiResponse> Edit([FromRoute] long id, [FromBody] EditPlatformCommand command)
        => mediator.Send(command with { Id = id }, CancellationToken.None);

    [HttpPost]
    public Task<ApiResponse> SetEnabled([FromBody] SetPlatformEnabledCommand command)
        => mediator.Send(command, CancellationToken.None);

    [HttpGet]
    public async Task<ApiResponse> Get([FromQuery] GetPlatformQuery query)
        => Ok(await mediator.Send(query, CancellationToken.None));
}
