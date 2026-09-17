using CommunalService.Domain;
using FluentValidation;
using MediatR;

namespace MarketingService.Application.Features.Reports;

/// <summary>活动效果报表查询：ActivityId&gt;0 时下钻该活动，From/To 为时间区间（含边界）。</summary>
/// <param name="ActivityId">活动 ID；0 表示只看全部活动汇总。</param>
/// <param name="From">起始时间（含）；null 不过滤。</param>
/// <param name="To">结束时间（含）；null 不过滤。</param>
/// <param name="Page">页码（下钻记录用）。</param>
/// <param name="PageSize">每页条数（下钻记录用）。</param>
public sealed record ActivityReportQuery(long ActivityId = 0, DateTime? From = null, DateTime? To = null, int Page = 1, int PageSize = 10)
    : IRequest<ApiResponse>;

/// <summary>券效果报表查询：CouponActivityId&gt;0 时下钻该券活动，From/To 为时间区间（含边界）。</summary>
/// <param name="CouponActivityId">券活动 ID；0 表示只看全部券活动汇总。</param>
/// <param name="From">起始时间（含）；null 不过滤。</param>
/// <param name="To">结束时间（含）；null 不过滤。</param>
/// <param name="Page">页码（下钻记录用）。</param>
/// <param name="PageSize">每页条数（下钻记录用）。</param>
public sealed record CouponReportQuery(long CouponActivityId = 0, DateTime? From = null, DateTime? To = null, int Page = 1, int PageSize = 10)
    : IRequest<ApiResponse>;

/// <summary>活动报表校验：分页范围与起止时间顺序。</summary>
public class ActivityReportValidator : AbstractValidator<ActivityReportQuery>
{
    /// <summary>规则覆盖：分页范围与起止时间顺序（To≥From）。</summary>
    public ActivityReportValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("页码不能小于1");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("每页数量必须为1-100");
        RuleFor(x => x.To)
            .Must((query, to) => !to.HasValue || !query.From.HasValue || to.Value >= query.From.Value)
            .WithMessage("结束时间不能早于开始时间");
    }
}

/// <summary>券报表校验：分页范围与起止时间顺序。</summary>
public class CouponReportValidator : AbstractValidator<CouponReportQuery>
{
    /// <summary>规则覆盖：分页范围与起止时间顺序（To≥From）。</summary>
    public CouponReportValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("页码不能小于1");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("每页数量必须为1-100");
        RuleFor(x => x.To)
            .Must((query, to) => !to.HasValue || !query.From.HasValue || to.Value >= query.From.Value)
            .WithMessage("结束时间不能早于开始时间");
    }
}
