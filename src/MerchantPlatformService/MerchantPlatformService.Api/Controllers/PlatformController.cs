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
    /// <summary>内部处理：List。</summary>
    public Task<ApiResponse> List([FromQuery] ListPlatformsQuery query)
        => mediator.Send(query, CancellationToken.None);

    [HttpPost]
    /// <summary>写操作：Create（副作用与幂等键见调用方约定）。</summary>
    public async Task<ApiResponse> Create([FromBody] CreatePlatformCommand command)
        => Ok(await mediator.Send(command, CancellationToken.None));

    [HttpPut("{id}")]
    /// <summary>内部处理：Edit。</summary>
    public Task<ApiResponse> Edit([FromRoute] long id, [FromBody] EditPlatformCommand command)
        => mediator.Send(command with { Id = id }, CancellationToken.None);

    [HttpPost]
    /// <summary>内部处理：SetEnabled。</summary>
    public Task<ApiResponse> SetEnabled([FromBody] SetPlatformEnabledCommand command)
        => mediator.Send(command, CancellationToken.None);

    [HttpGet]
    /// <summary>查询数据：Get（过滤条件与返回语义见参数与调用方约定）。</summary>
    public async Task<ApiResponse> Get([FromQuery] GetPlatformQuery query)
        => Ok(await mediator.Send(query, CancellationToken.None));
}
