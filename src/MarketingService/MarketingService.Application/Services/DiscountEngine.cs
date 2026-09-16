using CommunalService.Domain.Contracts.Messages;
using MarketingService.Domain.Entity;
using MarketingService.Domain.Enums;
using MarketingService.Domain.IRepository;
using Microsoft.Extensions.Logging;

namespace MarketingService.Application.Services;

/// <summary>
/// 优惠计算引擎（营销域核心）：逐商品贪心 + 平台配置的全局优先级（活动优先/券优先）+ 券与活动互斥。
///
/// 规则：
/// 1) 同一商品只能命中一个活动（取折扣最大者）；满赠（折扣为 0）只作为最后的兜底，避免阻挡真金白银的优惠。
/// 2) 同一商品只能使用一张券；一张券在一单中最多使用一次（按占用成功为准）。
/// 3) 优先级：券优先=先取最优券，无券才参加活动；活动优先反之。互斥由此天然保证。
/// 4) 0元减：抵扣金额必须小于商品行金额（券后价 > 0），避免出现零元支付。
/// 5) 所有折扣按商品行小计计算并四舍五入到分，单行最多抵扣到 0.01 元。
/// </summary>
public sealed class DiscountEngine(
    IMarketingActivityRepository activityRepository,
    IMarketingCouponRepository couponRepository,
    ILogger<DiscountEngine> logger)
{
    /// <summary>结算预览：无副作用，购物车/提交页展示浮动金额与可用券/活动。</summary>
    public async Task<MarketingPreviewResponse> PreviewAsync(MarketingSettleRequest request, CancellationToken cancellationToken)
    {
        var context = await BuildContextAsync(request, cancellationToken);
        var settle = Run(request, context);
        if (!settle.Success) return new MarketingPreviewResponse { Success = false, Message = settle.Message };

        var coupons = new List<MarketingAvailableCoupon>();
        foreach (var coupon in context.UsableCoupons)
        {
            var estimated = context.Items
                .Where(item => IsCouponApplicable(coupon, context.CouponActivities[coupon.CouponActivityId],
                    context.CouponTargets.GetValueOrDefault(coupon.CouponActivityId), item))
                .Select(item => CouponDiscount(coupon, context.Templates[coupon.CouponTemplateId], ItemAmount(item)))
                .DefaultIfEmpty(0m).Max();
            if (estimated <= 0) continue;
            var activity = context.CouponActivities[coupon.CouponActivityId];
            coupons.Add(new MarketingAvailableCoupon
            {
                UserCouponId = coupon.Id,
                CouponActivityId = coupon.CouponActivityId,
                Name = string.IsNullOrWhiteSpace(activity.Name) ? context.Templates[coupon.CouponTemplateId].Name : activity.Name,
                CouponType = context.Templates[coupon.CouponTemplateId].CouponType,
                Threshold = context.Templates[coupon.CouponTemplateId].Threshold,
                DiscountValue = context.Templates[coupon.CouponTemplateId].DiscountValue,
                ExpireAt = coupon.ExpireAt,
                EstimatedDiscount = estimated
            });
        }

        var activities = new List<MarketingAvailableActivity>();
        foreach (var activity in context.Activities)
        {
            var applicable = context.Items.Where(item => IsActivityApplicable(activity, context.ActivityTargets.GetValueOrDefault(activity.Id), item)).ToList();
            var estimated = applicable.Select(item => ActivityDiscount(activity, ItemAmount(item))).DefaultIfEmpty(0m).Max();
            var matched = applicable.Any(item => ItemAmount(item) >= activity.Threshold);
            if (!matched) continue;
            activities.Add(new MarketingAvailableActivity
            {
                ActivityId = activity.Id,
                Name = activity.Name,
                ActivityType = activity.ActivityType,
                Threshold = activity.Threshold,
                DiscountValue = activity.DiscountValue,
                EstimatedDiscount = estimated
            });
        }

        return new MarketingPreviewResponse
        {
            Success = true,
            ActivityDiscount = settle.ActivityDiscount,
            CouponDiscount = settle.CouponDiscount,
            TotalDiscount = settle.TotalDiscount,
            Items = settle.Items,
            Coupons = coupons.OrderByDescending(item => item.EstimatedDiscount).ToList(),
            Activities = activities.OrderByDescending(item => item.EstimatedDiscount).ToList()
        };
    }

    /// <summary>下单结算：计算并原子占用所选券；OrderNo 必填，占用后可用 Release 回退。</summary>
    public async Task<MarketingSettleResponse> SettleAsync(MarketingSettleRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OrderNo))
            return Fail("业务单号不能为空");

        var context = await BuildContextAsync(request, cancellationToken);
        var settle = Run(request, context);
        if (!settle.Success) return settle;

        // 逐张条件占用；任一张失败则把本单已占用的券全部退回，避免部分占用。
        var marked = new List<long>();
        foreach (var result in settle.Items.Where(item => item.HitType == (int)MarketingHitType.Coupon && item.UserCouponId > 0))
        {
            var ok = await couponRepository.MarkUserCouponUsedAsync(result.UserCouponId, request.OrderId, request.OrderNo);
            if (!ok)
            {
                foreach (var couponId in marked)
                    await couponRepository.ReleaseUserCouponAsync(request.OrderNo);
                logger.LogWarning("优惠券占用失败：UserCouponId={CouponId} OrderNo={OrderNo}", result.UserCouponId, request.OrderNo);
                return Fail("优惠券已被使用或已过期，请刷新后重试");
            }
            marked.Add(result.UserCouponId);
        }

        return settle;
    }

    private MarketingSettleResponse Run(MarketingSettleRequest request, CalculationContext context)
    {
        if (request.Items.Count == 0) return Fail("结算商品不能为空");
        if (request.Items.Any(item => item.Price <= 0 || item.Quantity <= 0)) return Fail("商品价格或数量不合法");
        // 用户勾选的券必须仍然有效：数量不一致说明有券被使用/过期，直接失败避免"少算券"误导用户。
        if (request.SelectedUserCouponIds is not null &&
            context.UsableCoupons.Count != request.SelectedUserCouponIds.Distinct().Count())
            return Fail("选择的优惠券不可用或已过期，请刷新后重试");

        var priority = (DiscountPriority)context.Priority;
        var usedCouponIds = new HashSet<long>();
        var results = new List<MarketingSettleItemResult>();

        // 折扣从大金额商品开始分配，有限的券优先覆盖优惠额更高的行。
        foreach (var item in context.Items.OrderByDescending(ItemAmount))
        {
            var amount = ItemAmount(item);

            MarketingActivity? bestActivity = null;
            var bestActivityDiscount = 0m;
            MarketingActivity? giftActivity = null;
            foreach (var activity in context.Activities)
            {
                if (!IsActivityApplicable(activity, context.ActivityTargets.GetValueOrDefault(activity.Id), item)) continue;
                if (amount < activity.Threshold) continue;
                if (activity.ActivityType == (int)ActivityType.GiftCoupon) { giftActivity ??= activity; continue; }
                var discount = ActivityDiscount(activity, amount);
                if (discount <= 0) continue;
                if (discount > bestActivityDiscount) { bestActivityDiscount = discount; bestActivity = activity; }
            }

            UserCoupon? bestCoupon = null;
            var bestCouponDiscount = 0m;
            foreach (var coupon in context.UsableCoupons)
            {
                if (usedCouponIds.Contains(coupon.Id)) continue;
                var activity = context.CouponActivities[coupon.CouponActivityId];
                if (!IsCouponApplicable(coupon, activity, context.CouponTargets.GetValueOrDefault(coupon.CouponActivityId), item)) continue;
                var template = context.Templates[coupon.CouponTemplateId];
                if (amount < template.Threshold) continue;
                var discount = CouponDiscount(coupon, template, amount);
                if (discount <= 0) continue;
                if (discount > bestCouponDiscount ||
                    (discount == bestCouponDiscount && bestCoupon is not null && coupon.ExpireAt < bestCoupon.ExpireAt))
                {
                    bestCouponDiscount = discount;
                    bestCoupon = coupon;
                }
            }

            var useCoupon = priority == DiscountPriority.CouponFirst ? bestCoupon is not null : bestCoupon is not null && bestActivity is null;
            var useActivity = !useCoupon && bestActivity is not null;
            var useGift = !useCoupon && !useActivity && giftActivity is not null;

            if (useCoupon && bestCoupon is not null)
            {
                usedCouponIds.Add(bestCoupon.Id);
                var activity = context.CouponActivities[bestCoupon.CouponActivityId];
                var template = context.Templates[bestCoupon.CouponTemplateId];
                results.Add(new MarketingSettleItemResult
                {
                    SkuId = item.SkuId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    HitType = (int)MarketingHitType.Coupon,
                    UserCouponId = bestCoupon.Id,
                    CouponActivityId = bestCoupon.CouponActivityId,
                    CouponName = string.IsNullOrWhiteSpace(activity.Name) ? template.Name : activity.Name,
                    DiscountAmount = bestCouponDiscount,
                    ItemAmount = amount,
                    PayAmount = amount - bestCouponDiscount
                });
                continue;
            }

            if (useActivity && bestActivity is not null)
            {
                results.Add(new MarketingSettleItemResult
                {
                    SkuId = item.SkuId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    HitType = (int)MarketingHitType.Activity,
                    ActivityId = bestActivity.Id,
                    ActivityName = bestActivity.Name,
                    ActivityType = bestActivity.ActivityType,
                    DiscountAmount = bestActivityDiscount,
                    ItemAmount = amount,
                    PayAmount = amount - bestActivityDiscount
                });
                continue;
            }

            if (useGift && giftActivity is not null)
            {
                results.Add(new MarketingSettleItemResult
                {
                    SkuId = item.SkuId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    HitType = (int)MarketingHitType.Activity,
                    ActivityId = giftActivity.Id,
                    ActivityName = giftActivity.Name,
                    ActivityType = giftActivity.ActivityType,
                    GiftCouponActivityId = giftActivity.GiftCouponActivityId,
                    DiscountAmount = 0m,
                    ItemAmount = amount,
                    PayAmount = amount
                });
                continue;
            }

            results.Add(new MarketingSettleItemResult
            {
                SkuId = item.SkuId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                HitType = (int)MarketingHitType.None,
                ItemAmount = amount,
                PayAmount = amount
            });
        }

        var activityDiscount = results.Where(item => item.HitType == (int)MarketingHitType.Activity).Sum(item => item.DiscountAmount);
        var couponDiscount = results.Where(item => item.HitType == (int)MarketingHitType.Coupon).Sum(item => item.DiscountAmount);
        return new MarketingSettleResponse
        {
            Success = true,
            ActivityDiscount = Round2(activityDiscount),
            CouponDiscount = Round2(couponDiscount),
            TotalDiscount = Round2(activityDiscount + couponDiscount),
            Items = results
        };
    }

    private async Task<CalculationContext> BuildContextAsync(MarketingSettleRequest request, CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var config = await activityRepository.GetConfigAsync(request.PlatformId);
        var activities = await activityRepository.ListEnabledAsync(request.PlatformId, now);
        var activityTargets = (await activityRepository.ListTargetsAsync(activities.Select(item => item.Id).ToList()))
            .GroupBy(item => item.ActivityId).ToDictionary(group => group.Key, group => group.ToList());

        var userCoupons = request.UserId > 0
            ? await couponRepository.ListUserCouponsAsync(request.UserId, unusedOnly: true)
            : [];
        if (request.SelectedUserCouponIds is not null)
        {
            var selected = request.SelectedUserCouponIds.ToHashSet();
            var matched = userCoupons.Where(item => selected.Contains(item.Id)).ToList();
            userCoupons = matched;
        }

        var couponActivityIds = userCoupons.Select(item => item.CouponActivityId).Distinct().ToList();
        var couponActivities = (await couponRepository.ListActivitiesByIdsAsync(couponActivityIds)).ToDictionary(item => item.Id);
        var templates = (await couponRepository.ListTemplatesByIdsAsync(userCoupons.Select(item => item.CouponTemplateId).Distinct().ToList())).ToDictionary(item => item.Id);
        var couponTargets = (await couponRepository.ListTargetsAsync(couponActivityIds))
            .GroupBy(item => item.CouponActivityId).ToDictionary(group => group.Key, group => group.ToList());

        return new CalculationContext
        {
            Priority = config?.DiscountPriority ?? (int)DiscountPriority.CouponFirst,
            Activities = activities,
            ActivityTargets = activityTargets,
            UsableCoupons = userCoupons.Where(item => couponActivities.ContainsKey(item.CouponActivityId) && templates.ContainsKey(item.CouponTemplateId)).ToList(),
            CouponActivities = couponActivities,
            CouponTargets = couponTargets,
            Templates = templates,
            Items = request.Items
        };
    }

    private static bool IsActivityApplicable(MarketingActivity activity, List<MarketingActivityTarget>? targets, MarketingSettleItem item)
    {
        if (activity.PlatformId != item.PlatformId) return false;
        if (activity.MerchantId > 0 && activity.MerchantId != item.MerchantId) return false;
        return (MarketingScopeType)activity.ScopeType switch
        {
            MarketingScopeType.All => true,
            MarketingScopeType.Merchant => targets?.Any(target =>
                target.TargetType == (int)MarketingTargetType.Merchant && target.TargetId == item.MerchantId) == true,
            MarketingScopeType.Products => targets?.Any(target =>
                target.TargetType == (int)MarketingTargetType.Product && target.TargetId == item.SkuId) == true,
            _ => false
        };
    }

    private static bool IsCouponApplicable(UserCoupon coupon, CouponActivity activity, List<CouponActivityTarget>? targets, MarketingSettleItem item)
    {
        if (coupon.PlatformId != item.PlatformId) return false;
        if (activity.MerchantId > 0 && activity.MerchantId != item.MerchantId) return false;
        return (MarketingScopeType)activity.ScopeType switch
        {
            MarketingScopeType.All => true,
            MarketingScopeType.Merchant => targets?.Any(target =>
                target.TargetType == (int)MarketingTargetType.Merchant && target.TargetId == item.MerchantId) == true,
            MarketingScopeType.Products => targets?.Any(target =>
                target.TargetType == (int)MarketingTargetType.Product && target.TargetId == item.SkuId) == true,
            _ => false
        };
    }

    private static decimal ActivityDiscount(MarketingActivity activity, decimal amount) => activity.ActivityType switch
    {
        (int)ActivityType.FullReduce => Cap(activity.DiscountValue, amount),
        (int)ActivityType.FullDiscount => amount >= activity.Threshold ? Cap(amount * (1 - activity.DiscountValue), amount) : 0m,
        _ => 0m
    };

    private static decimal CouponDiscount(UserCoupon coupon, CouponTemplate template, decimal amount)
        => template.CouponType switch
        {
            // 0元减：优惠额必须小于商品行金额，保证券后价 > 0。
            (int)CouponType.NoThresholdReduce => template.DiscountValue < amount ? Round2(template.DiscountValue) : 0m,
            (int)CouponType.FullReduce => amount >= template.Threshold ? Cap(template.DiscountValue, amount) : 0m,
            (int)CouponType.FullDiscount => amount >= template.Threshold ? Cap(amount * (1 - template.DiscountValue), amount) : 0m,
            _ => 0m
        };

    private static decimal Cap(decimal discount, decimal amount) => Round2(Math.Min(discount, Math.Max(amount - 0.01m, 0m)));

    private static decimal ItemAmount(MarketingSettleItem item) => Round2(item.Price * item.Quantity);

    private static decimal Round2(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static MarketingSettleResponse Fail(string message) => new() { Success = false, Message = message };

    private sealed class CalculationContext
    {
        public int Priority { get; init; }
        public List<MarketingActivity> Activities { get; init; } = [];
        public Dictionary<long, List<MarketingActivityTarget>> ActivityTargets { get; init; } = [];
        public List<UserCoupon> UsableCoupons { get; init; } = [];
        public Dictionary<long, CouponActivity> CouponActivities { get; init; } = [];
        public Dictionary<long, List<CouponActivityTarget>> CouponTargets { get; init; } = [];
        public Dictionary<long, CouponTemplate> Templates { get; init; } = [];
        public List<MarketingSettleItem> Items { get; init; } = [];
    }
}
