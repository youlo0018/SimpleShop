using CommunalService.Domain;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MarketingService.Application.Features.Activities;
using MarketingService.Application.Features.Reports;
using MarketingService.Application.Features.UserMarketing;

namespace MarketingService.Api.Controllers;

/// <summary>
/// 营销入口：控制器只做协议转换；后台管理动作的租户权限在网关（marketing:read/create），
/// 用户自助动作（领券/券包/结算预览）依赖登录态并额外在 Handler 校验。
/// </summary>
public class MarketingController(IMediator mediator) : BaseController
{
    [HttpGet]
    public Task<ApiResponse> ActivityList([FromQuery] ListActivitiesQuery query)
        => mediator.Send(query, CancellationToken.None);

    [HttpGet]
    public Task<ApiResponse> ActivityDetail([FromQuery] GetActivityQuery query)
        => mediator.Send(query, CancellationToken.None);

    [HttpPost]
    public Task<ApiResponse> SaveActivity([FromBody] SaveActivityCommand command)
        => mediator.Send(command, CancellationToken.None);

    [HttpPost]
    public Task<ApiResponse> SetActivityEnabled([FromBody] SetActivityEnabledCommand command)
        => mediator.Send(command, CancellationToken.None);

    [HttpGet]
    public Task<ApiResponse> CouponTemplateList([FromQuery] ListCouponTemplatesQuery query)
        => mediator.Send(query, CancellationToken.None);

    [HttpPost]
    public Task<ApiResponse> SaveCouponTemplate([FromBody] SaveCouponTemplateCommand command)
        => mediator.Send(command, CancellationToken.None);

    [HttpPost]
    public Task<ApiResponse> SetCouponTemplateEnabled([FromBody] SetCouponTemplateEnabledCommand command)
        => mediator.Send(command, CancellationToken.None);

    [HttpGet]
    public Task<ApiResponse> CouponActivityList([FromQuery] ListCouponActivitiesQuery query)
        => mediator.Send(query, CancellationToken.None);

    [HttpGet]
    public Task<ApiResponse> CouponActivityDetail([FromQuery] GetCouponActivityQuery query)
        => mediator.Send(query, CancellationToken.None);

    [HttpPost]
    public Task<ApiResponse> SaveCouponActivity([FromBody] SaveCouponActivityCommand command)
        => mediator.Send(command, CancellationToken.None);

    [HttpPost]
    public Task<ApiResponse> SetCouponActivityEnabled([FromBody] SetCouponActivityEnabledCommand command)
        => mediator.Send(command, CancellationToken.None);

    [HttpGet]
    public Task<ApiResponse> Config([FromQuery] GetMarketingConfigQuery query)
        => mediator.Send(query, CancellationToken.None);

    [HttpPost]
    public Task<ApiResponse> SaveConfig([FromBody] SaveMarketingConfigCommand command)
        => mediator.Send(command, CancellationToken.None);

    [HttpGet]
    public Task<ApiResponse> ActivityReport([FromQuery] ActivityReportQuery query)
        => mediator.Send(query, CancellationToken.None);

    [HttpGet]
    public Task<ApiResponse> CouponReport([FromQuery] CouponReportQuery query)
        => mediator.Send(query, CancellationToken.None);

    [HttpGet]
    public Task<ApiResponse> ClaimableCoupons([FromQuery] ClaimableCouponsQuery query)
        => mediator.Send(query, CancellationToken.None);

    [HttpPost]
    public Task<ApiResponse> ClaimCoupon([FromBody] ClaimCouponCommand command)
        => mediator.Send(command, CancellationToken.None);

    [HttpGet]
    public Task<ApiResponse> MyCoupons([FromQuery] MyCouponsQuery query)
        => mediator.Send(query, CancellationToken.None);

    [HttpPost]
    public Task<ApiResponse> SettlePreview([FromBody] SettlePreviewCommand command)
        => mediator.Send(command, CancellationToken.None);
}
