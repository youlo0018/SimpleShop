using CommunalService.Domain;
using CommunalService.Domain.Enums;
using MediatR;
using MarketingService.Domain.IRepository;

namespace MarketingService.Application.Features.Reports;

/// <summary>
/// 活动效果报表：按活动聚合参与订单数/折扣总额/赠券张数；指定 ActivityId 时下钻该活动的订单记录，
/// 并把当前页记录的逐商品明细一并返回，供后台核对每个商品的折扣。
/// </summary>
public class ActivityReportHandler(IMarketingActivityRepository repository, TenantContext tenant)
    : IRequestHandler<ActivityReportQuery, ApiResponse>
{
    /// <summary>活动效果报表：按活动聚合参与订单数/折扣总额/赠券数；传 ActivityId 时下钻该活动的订单记录与商品明细。</summary>
    public async Task<ApiResponse> Handle(ActivityReportQuery request, CancellationToken cancellationToken)
    {
        var platformId = tenant.IsPlatform ? tenant.PlatformId : 0;
        var merchantId = tenant.IsMerchant ? tenant.MerchantId : 0;

        var summary = await repository.SummarizeActivitiesAsync(platformId, merchantId, request.From, request.To);
        var (records, total) = await repository.QueryRecordsAsync(
            platformId, merchantId, request.ActivityId, request.From, request.To, request.Page, request.PageSize);
        var items = await repository.ListRecordItemsAsync(records.Select(item => item.Id).ToList());

        return ApiResults.Ok(new
        {
            summary,
            records,
            recordItems = items,
            total,
            page = request.Page,
            pageSize = request.PageSize,
            totalDiscount = summary.Sum(item => item.DiscountAmount),
            totalOrders = summary.Sum(item => item.OrderCount)
        });
    }
}

/// <summary>券效果报表：按券活动聚合核销笔数与抵扣总额；指定 CouponActivityId 时下钻用券记录明细。</summary>
public class CouponReportHandler(IMarketingCouponRepository repository, TenantContext tenant)
    : IRequestHandler<CouponReportQuery, ApiResponse>
{
    /// <summary>券效果报表：按券活动聚合核销笔数/抵扣总额；传 CouponActivityId 时下钻用券记录与商品明细。</summary>
    public async Task<ApiResponse> Handle(CouponReportQuery request, CancellationToken cancellationToken)
    {
        var platformId = tenant.IsPlatform ? tenant.PlatformId : 0;
        var merchantId = tenant.IsMerchant ? tenant.MerchantId : 0;

        var summary = await repository.SummarizeCouponsAsync(platformId, merchantId, request.From, request.To);
        var (records, total) = await repository.QueryCouponRecordsAsync(
            platformId, merchantId, request.CouponActivityId, request.From, request.To, request.Page, request.PageSize);
        var items = await repository.ListCouponRecordItemsAsync(records.Select(item => item.Id).ToList());

        return ApiResults.Ok(new
        {
            summary,
            records,
            recordItems = items,
            total,
            page = request.Page,
            pageSize = request.PageSize,
            totalDiscount = summary.Sum(item => item.DiscountAmount),
            totalCoupons = summary.Sum(item => item.CouponCount)
        });
    }
}
