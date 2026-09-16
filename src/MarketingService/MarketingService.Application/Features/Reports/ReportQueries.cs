using CommunalService.Domain;
using FluentValidation;
using MediatR;

namespace MarketingService.Application.Features.Reports;

public sealed record ActivityReportQuery(long ActivityId = 0, DateTime? From = null, DateTime? To = null, int Page = 1, int PageSize = 10)
    : IRequest<ApiResponse>;

public sealed record CouponReportQuery(long CouponActivityId = 0, DateTime? From = null, DateTime? To = null, int Page = 1, int PageSize = 10)
    : IRequest<ApiResponse>;

public class ActivityReportValidator : AbstractValidator<ActivityReportQuery>
{
    public ActivityReportValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("页码不能小于1");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("每页数量必须为1-100");
        RuleFor(x => x.To)
            .Must((query, to) => !to.HasValue || !query.From.HasValue || to.Value >= query.From.Value)
            .WithMessage("结束时间不能早于开始时间");
    }
}

public class CouponReportValidator : AbstractValidator<CouponReportQuery>
{
    public CouponReportValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("页码不能小于1");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("每页数量必须为1-100");
        RuleFor(x => x.To)
            .Must((query, to) => !to.HasValue || !query.From.HasValue || to.Value >= query.From.Value)
            .WithMessage("结束时间不能早于开始时间");
    }
}
