using CommunalService.Domain;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MerchantPlatformService.Application.Features.Merchants.Create;
using MerchantPlatformService.Application.Features.Merchants.Get;
using MerchantPlatformService.Application.Features.Merchants.List;
using MerchantPlatformService.Application.Features.Merchants.Review;
using MerchantPlatformService.Application.Features.Merchants.SetStatus;
using MerchantPlatformService.Application.Features.Merchants.Shop;
using MerchantPlatformService.Application.Features.Merchants.Update;

namespace MerchantPlatformService.Api.Controllers;

/// <summary>
/// 商户与店铺入口：控制器只做协议转换（查询参数/请求体 → Command/Query），
/// 分页校验、租户裁剪、审核结论归一化全部在 Application 层（Validator/Handler）。
/// </summary>
public class MerchantController(IMediator mediator) : BaseController
{
    /// <summary>后台商户分页列表（GET，merchant:read）：平台看本平台、商户看本商户。</summary>
    [HttpGet]
    public async Task<ApiResponse> List([FromQuery] ListMerchantsQuery query)
        => Ok(await mediator.Send(query, CancellationToken.None));

    /// <summary>商户入驻申请（POST，merchant:create）：商户账号禁止创建商户，平台账号强制本平台。</summary>
    [HttpPost]
    public async Task<ApiResponse> Create([FromBody] CreateMerchantCommand command)
        => Ok(await mediator.Send(command, CancellationToken.None));

    /// <summary>店铺主页公开信息（GET，游客可访问）：只返回已入驻商户的展示字段。</summary>
    [HttpGet]
    public async Task<ApiResponse> Shop([FromQuery] GetShopQuery query)
        => Ok(await mediator.Send(query, CancellationToken.None));

    /// <summary>商户详情（GET，merchant:read）。</summary>
    [HttpGet]
    public async Task<ApiResponse> Get([FromQuery] GetMerchantQuery query)
        => Ok(await mediator.Send(query, CancellationToken.None));

    /// <summary>商户审核（POST，merchant:review）：Approved 优先，兼容旧前端 Status=10。</summary>
    [HttpPost]
    public async Task<ApiResponse> Review([FromBody] ReviewMerchantCommand command)
        => Ok(await mediator.Send(command, CancellationToken.None));

    /// <summary>商户资料更新（POST，merchant:update）。</summary>
    [HttpPost]
    public async Task<ApiResponse> Update([FromBody] UpdateMerchantCommand command)
        => Ok(await mediator.Send(command, CancellationToken.None));

    /// <summary>商户启停（POST，merchant:update）。</summary>
    [HttpPost]
    public async Task<ApiResponse> SetStatus([FromBody] SetMerchantStatusCommand command)
        => Ok(await mediator.Send(command, CancellationToken.None));
}
