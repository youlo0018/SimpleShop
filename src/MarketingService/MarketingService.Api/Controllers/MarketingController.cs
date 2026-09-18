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
{    /// <summary>活动列表（GET /gateway/marketing/ActivityList）：后台分页，网关按 marketing:read 放行。</summary>
    /// <summary>内部处理：ActivityList。</summary>
    public Task<ApiResponse> ActivityList([FromQuery] ListActivitiesQuery query)
        => mediator.Send(query, CancellationToken.None);
    /// <summary>活动详情：含范围明细，编辑弹窗回显用。</summary>
    public Task<ApiResponse> ActivityDetail([FromQuery] GetActivityQuery query)
        => mediator.Send(query, CancellationToken.None);
    /// <summary>新建/编辑活动（权限 marketing:create）：满赠需选择同租户券活动。</summary>
    public Task<ApiResponse> SaveActivity([FromBody] SaveActivityCommand command)
        => mediator.Send(command, CancellationToken.None);
    /// <summary>活动启停（权限 marketing:create）。</summary>
    public Task<ApiResponse> SetActivityEnabled([FromBody] SetActivityEnabledCommand command)
        => mediator.Send(command, CancellationToken.None);
    /// <summary>券模板列表（GET）：后台分页。</summary>
    public Task<ApiResponse> CouponTemplateList([FromQuery] ListCouponTemplatesQuery query)
        => mediator.Send(query, CancellationToken.None);
    /// <summary>新建/编辑券模板（权限 marketing:create）。</summary>
    public Task<ApiResponse> SaveCouponTemplate([FromBody] SaveCouponTemplateCommand command)
        => mediator.Send(command, CancellationToken.None);
    /// <summary>券模板启停（权限 marketing:create）。</summary>
    public Task<ApiResponse> SetCouponTemplateEnabled([FromBody] SetCouponTemplateEnabledCommand command)
        => mediator.Send(command, CancellationToken.None);
    /// <summary>券活动列表（GET）：附带模板名称与优惠信息。</summary>
    public Task<ApiResponse> CouponActivityList([FromQuery] ListCouponActivitiesQuery query)
        => mediator.Send(query, CancellationToken.None);
    /// <summary>券活动详情：含范围与模板。</summary>
    public Task<ApiResponse> CouponActivityDetail([FromQuery] GetCouponActivityQuery query)
        => mediator.Send(query, CancellationToken.None);
    /// <summary>新建/编辑券活动（权限 marketing:create）：模板必须属于当前租户。</summary>
    public Task<ApiResponse> SaveCouponActivity([FromBody] SaveCouponActivityCommand command)
        => mediator.Send(command, CancellationToken.None);
    /// <summary>券活动启停（权限 marketing:create）。</summary>
    public Task<ApiResponse> SetCouponActivityEnabled([FromBody] SetCouponActivityEnabledCommand command)
        => mediator.Send(command, CancellationToken.None);
    /// <summary>平台营销配置（GET）：券/活动计算优先级。</summary>
    public Task<ApiResponse> Config([FromQuery] GetMarketingConfigQuery query)
        => mediator.Send(query, CancellationToken.None);
    /// <summary>保存营销配置（权限 marketing:create，仅平台账号）。</summary>
    public Task<ApiResponse> SaveConfig([FromBody] SaveMarketingConfigCommand command)
        => mediator.Send(command, CancellationToken.None);
    /// <summary>活动效果报表（GET，marketing:read）：汇总 + 活动下钻明细。</summary>
    public Task<ApiResponse> ActivityReport([FromQuery] ActivityReportQuery query)
        => mediator.Send(query, CancellationToken.None);
    /// <summary>券效果报表（GET，marketing:read）：汇总 + 券活动下钻明细。</summary>
    public Task<ApiResponse> CouponReport([FromQuery] CouponReportQuery query)
        => mediator.Send(query, CancellationToken.None);

    /// <summary>进行中活动（GET，游客可访问）：首页优惠专区与活动卡，返回活动与可领取券活动。</summary>
    [HttpGet]
    public Task<ApiResponse> ActiveActivities([FromQuery] ActiveActivitiesQuery query)
        => mediator.Send(query, CancellationToken.None);

    /// <summary>领券中心（GET，登录用户放行）：当前平台可领取的券活动与可领状态。</summary>
    [HttpGet]
    public Task<ApiResponse> ClaimableCoupons([FromQuery] ClaimableCouponsQuery query)
        => mediator.Send(query, CancellationToken.None);
    /// <summary>领取优惠券（POST，登录用户）：限领与库存双重校验，库存条件自增防超发。</summary>
    public Task<ApiResponse> ClaimCoupon([FromBody] ClaimCouponCommand command)
        => mediator.Send(command, CancellationToken.None);
    /// <summary>我的券包（GET，登录用户）：支持按状态过滤，过期券按已过期展示。</summary>
    public Task<ApiResponse> MyCoupons([FromQuery] MyCouponsQuery query)
        => mediator.Send(query, CancellationToken.None);
    /// <summary>结算预览（POST，登录用户）：计算优惠与可用券/活动，无副作用（下单前展示用）。</summary>
    public Task<ApiResponse> SettlePreview([FromBody] SettlePreviewCommand command)
        => mediator.Send(command, CancellationToken.None);
    /// <summary>到手价试算（POST，游客可访问）：商品列表/详情批量展示"到手价"，口径与结算引擎一致。</summary>
    public Task<ApiResponse> FinalPrice([FromBody] FinalPriceCommand command)
        => mediator.Send(command, CancellationToken.None);
}
