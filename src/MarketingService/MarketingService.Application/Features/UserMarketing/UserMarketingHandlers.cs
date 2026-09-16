using CommunalService.Domain;
using CommunalService.Domain.Enums;
using MediatR;
using MarketingService.Application.Services;
using MarketingService.Domain.Entity;
using MarketingService.Domain.Enums;
using MarketingService.Domain.IRepository;

namespace MarketingService.Application.Features.UserMarketing;

/// <summary>
/// 领券中心：列出本平台可领取的券活动（在有效期内、启用、可领取、有库存），并标记当前用户是否还能领。
/// </summary>
public class ClaimableCouponsHandler(IMarketingCouponRepository repository, TenantContext tenant)
    : IRequestHandler<ClaimableCouponsQuery, ApiResponse>
{
    public async Task<ApiResponse> Handle(ClaimableCouponsQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var activities = (await repository.ListEnabledActivitiesAsync(request.PlatformId, now))
            .Where(item => item.IsClaimable).ToList();
        if (activities.Count == 0) return ApiResults.Ok(new { items = Array.Empty<object>() });

        var templates = (await repository.ListTemplatesByIdsAsync(activities.Select(item => item.CouponTemplateId).Distinct().ToList()))
            .ToDictionary(item => item.Id);
        var userId = tenant.UserId;
        var items = new List<object>();
        foreach (var activity in activities)
        {
            var template = templates.GetValueOrDefault(activity.CouponTemplateId);
            if (template is null) continue;
            // 领券中心不是券活动使用范围展示位，这里只做"能否领取"判定：库存 + 每人限领。
            var claimed = userId > 0 ? await repository.CountUserIssuedAsync(activity.Id, userId) : 0;
            items.Add(new
            {
                activity.Id,
                activity.Name,
                activity.CouponTemplateId,
                TemplateName = template.Name,
                TemplateDescription = template.Description,
                CouponType = template.CouponType,
                Threshold = template.Threshold,
                DiscountValue = template.DiscountValue,
                ValidDays = template.ValidDays,
                activity.TotalStock,
                activity.IssuedCount,
                RemainingStock = Math.Max(0, activity.TotalStock - activity.IssuedCount),
                activity.PerUserLimit,
                ClaimedCount = claimed,
                CanClaim = activity.IssuedCount < activity.TotalStock && claimed < activity.PerUserLimit,
                activity.EndAt
            });
        }
        return ApiResults.Ok(new { items });
    }
}

/// <summary>
/// 领取优惠券：库存与限领在发放入口校验（条件更新 +1 防超发），成功后生成用户券（领取后 N 天有效）。
/// </summary>
public class ClaimCouponHandler(IMarketingCouponRepository repository, TenantContext tenant)
    : IRequestHandler<ClaimCouponCommand, ApiResponse>
{
    public async Task<ApiResponse> Handle(ClaimCouponCommand request, CancellationToken cancellationToken)
    {
        if (tenant.UserId <= 0) return ApiResults.Fail(BaseApiResponseCode.Unauthorized, "请先登录");

        var activity = await repository.GetActivityByIdAsync(request.CouponActivityId);
        if (activity is null || !activity.IsEnabled || !activity.IsClaimable)
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "优惠券不存在或暂不可领取");
        var now = DateTime.Now;
        if (activity.StartAt > now || (activity.EndAt.HasValue && activity.EndAt <= now))
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "优惠券不在领取时间内");

        var template = (await repository.ListTemplatesByIdsAsync([activity.CouponTemplateId])).FirstOrDefault();
        if (template is null || !template.IsEnabled)
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "优惠券模板已停用");

        var claimed = await repository.CountUserIssuedAsync(activity.Id, tenant.UserId);
        if (claimed >= activity.PerUserLimit)
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "已达到每人限领数量");

        if (!await repository.TryIncreaseIssuedAsync(activity.Id))
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "优惠券已被领完");

        var userCoupon = new UserCoupon
        {
            UserId = tenant.UserId,
            CouponActivityId = activity.Id,
            CouponTemplateId = activity.CouponTemplateId,
            PlatformId = activity.PlatformId,
            MerchantId = activity.MerchantId,
            Status = (int)UserCouponStatus.Unused,
            Source = (int)UserCouponSource.Claim,
            ReceivedAt = now,
            ExpireAt = now.AddDays(template.ValidDays)
        };
        await repository.InsertUserCouponAsync(userCoupon);
        return ApiResults.Ok(new { success = true, userCouponId = userCoupon.Id, expireAt = userCoupon.ExpireAt });
    }
}

/// <summary>我的券包：返回券的活动/模板展示信息；未使用但已过期的按"已过期"展示。</summary>
public class MyCouponsHandler(IMarketingCouponRepository repository, TenantContext tenant)
    : IRequestHandler<MyCouponsQuery, ApiResponse>
{
    public async Task<ApiResponse> Handle(MyCouponsQuery request, CancellationToken cancellationToken)
    {
        if (tenant.UserId <= 0) return ApiResults.Fail(BaseApiResponseCode.Unauthorized, "请先登录");

        var coupons = await repository.ListUserCouponsAsync(tenant.UserId, unusedOnly: false);
        var activities = (await repository.ListActivitiesByIdsAsync(coupons.Select(item => item.CouponActivityId).Distinct().ToList()))
            .ToDictionary(item => item.Id);
        var templates = (await repository.ListTemplatesByIdsAsync(coupons.Select(item => item.CouponTemplateId).Distinct().ToList()))
            .ToDictionary(item => item.Id);

        var now = DateTime.Now;
        var items = coupons
            .Select(coupon =>
            {
                var displayStatus = coupon.Status == (int)UserCouponStatus.Unused && coupon.ExpireAt <= now
                    ? (int)UserCouponStatus.Expired
                    : coupon.Status;
                var template = templates.GetValueOrDefault(coupon.CouponTemplateId);
                var activity = activities.GetValueOrDefault(coupon.CouponActivityId);
                return new
                {
                    coupon.Id,
                    coupon.CouponActivityId,
                    ActivityName = activity?.Name ?? string.Empty,
                    TemplateName = template?.Name ?? string.Empty,
                    CouponType = template?.CouponType ?? 0,
                    Threshold = template?.Threshold ?? 0m,
                    DiscountValue = template?.DiscountValue ?? 0m,
                    coupon.PlatformId,
                    coupon.MerchantId,
                    coupon.Source,
                    Status = displayStatus,
                    coupon.ReceivedAt,
                    coupon.ExpireAt,
                    coupon.UsedOrderNo
                };
            })
            .Where(item => request.Status is null || item.Status == request.Status)
            .OrderBy(item => item.Status)
            .ThenByDescending(item => item.ReceivedAt)
            .ToList();
        return ApiResults.Ok(new { items });
    }
}

/// <summary>购物车/提交页结算预览：调用优惠引擎，返回逐商品优惠与可用券/活动清单。</summary>
public class SettlePreviewHandler(DiscountEngine engine, TenantContext tenant)
    : IRequestHandler<SettlePreviewCommand, ApiResponse>
{
    public async Task<ApiResponse> Handle(SettlePreviewCommand request, CancellationToken cancellationToken)
    {
        var userId = tenant.UserId > 0 ? tenant.UserId : 0;
        var result = await engine.PreviewAsync(request.ToRequest(userId), cancellationToken);
        return result.Success
            ? ApiResults.Ok(result)
            : ApiResults.Fail(BaseApiResponseCode.BadRequest, result.Message);
    }
}
