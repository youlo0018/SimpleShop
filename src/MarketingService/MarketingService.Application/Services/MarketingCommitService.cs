using CommunalService.Domain.Contracts.Messages;
using MarketingService.Domain.Entity;
using MarketingService.Domain.Enums;
using MarketingService.Domain.IRepository;
using Microsoft.Extensions.Logging;

namespace MarketingService.Application.Services;

/// <summary>
/// 营销落账服务：订单落库后写活动/用券记录（按订单号幂等）；支付成功后发放满赠券；订单取消时回退券占用。
/// </summary>
public sealed class MarketingCommitService(
    IMarketingActivityRepository activityRepository,
    IMarketingCouponRepository couponRepository,
    ILogger<MarketingCommitService> logger)
{
    public async Task<MarketingCommitResponse> CommitAsync(MarketingCommitRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OrderNo))
            return new MarketingCommitResponse { Success = false, Message = "缺少订单号" };
        if (!request.Settle.Success)
            return new MarketingCommitResponse { Success = false, Message = "结算结果无效" };

        // 幂等：同一订单重复提交（重试/补发）不重复落账。
        if (await activityRepository.HasRecordAsync(request.OrderNo) || await couponRepository.HasCouponRecordAsync(request.OrderNo))
            return new MarketingCommitResponse { Success = true };

        var activityHits = request.Settle.Items
            .Where(item => item.HitType == (int)MarketingHitType.Activity && item.ActivityId > 0)
            .ToList();
        foreach (var group in activityHits.GroupBy(item => item.ActivityId))
        {
            var first = group.First();
            var activity = await activityRepository.GetByIdAsync(group.Key);
            var record = new MarketingActivityRecord
            {
                ActivityId = group.Key,
                ActivityName = string.IsNullOrWhiteSpace(first.ActivityName) ? activity?.Name ?? string.Empty : first.ActivityName,
                ActivityType = first.ActivityType,
                PlatformId = activity?.PlatformId ?? request.PlatformId,
                MerchantId = activity?.MerchantId ?? 0,
                OrderId = request.OrderId,
                OrderNo = request.OrderNo,
                UserId = request.UserId,
                DiscountAmount = group.Sum(item => item.DiscountAmount),
                GiftCouponCount = 0
            };
            var items = group.Select(item => new MarketingActivityRecordItem
            {
                RecordId = record.Id,
                ActivityId = group.Key,
                OrderNo = request.OrderNo,
                SkuId = item.SkuId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                ItemAmount = item.ItemAmount,
                DiscountAmount = item.DiscountAmount
            }).ToList();
            await activityRepository.InsertRecordAsync(record, items);
        }

        var couponHits = request.Settle.Items
            .Where(item => item.HitType == (int)MarketingHitType.Coupon && item.UserCouponId > 0)
            .ToList();
        if (couponHits.Count > 0)
        {
            var couponIds = couponHits.Select(item => item.UserCouponId).Distinct().ToList();
            var userCoupons = (await couponRepository.ListUserCouponsByIdsAsync(request.UserId, couponIds)).ToDictionary(item => item.Id);
            foreach (var group in couponHits.GroupBy(item => item.UserCouponId))
            {
                var first = group.First();
                var userCoupon = userCoupons.GetValueOrDefault(group.Key);
                var record = new CouponRecord
                {
                    CouponActivityId = first.CouponActivityId,
                    CouponActivityName = first.CouponName,
                    CouponTemplateId = userCoupon?.CouponTemplateId ?? 0,
                    UserCouponId = group.Key,
                    PlatformId = userCoupon?.PlatformId ?? request.PlatformId,
                    MerchantId = userCoupon?.MerchantId ?? 0,
                    OrderId = request.OrderId,
                    OrderNo = request.OrderNo,
                    UserId = request.UserId,
                    DiscountAmount = group.Sum(item => item.DiscountAmount)
                };
                var items = group.Select(item => new CouponRecordItem
                {
                    RecordId = record.Id,
                    CouponActivityId = first.CouponActivityId,
                    OrderNo = request.OrderNo,
                    SkuId = item.SkuId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    ItemAmount = item.ItemAmount,
                    DiscountAmount = item.DiscountAmount
                }).ToList();
                await couponRepository.InsertCouponRecordAsync(record, items);
            }
        }

        return new MarketingCommitResponse { Success = true };
    }

    /// <summary>
    /// 满赠发券（支付成功后触发）：按订单的活动记录找到满赠活动，向用户券包发放指定券活动的券。
    /// 每单每活动只发一次（GiftCouponCount 幂等），库存用条件更新防超发。
    /// </summary>
    public async Task IssueGiftCouponsAsync(string orderNo, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(orderNo)) return;
        var records = await activityRepository.ListRecordsByOrderNoAsync(orderNo);
        foreach (var record in records.Where(item => item.ActivityType == (int)ActivityType.GiftCoupon && item.GiftCouponCount == 0))
        {
            var activity = await activityRepository.GetByIdAsync(record.ActivityId);
            if (activity is null || activity.GiftCouponActivityId <= 0) continue;
            var couponActivity = await couponRepository.GetActivityByIdAsync(activity.GiftCouponActivityId);
            if (couponActivity is null || !couponActivity.IsEnabled) continue;
            var template = (await couponRepository.ListTemplatesByIdsAsync([couponActivity.CouponTemplateId])).FirstOrDefault();
            if (template is null || !template.IsEnabled) continue;

            if (!await couponRepository.TryIncreaseIssuedAsync(couponActivity.Id))
            {
                logger.LogWarning("满赠发券失败（库存不足）：OrderNo={OrderNo} CouponActivityId={ActivityId}", orderNo, couponActivity.Id);
                continue;
            }

            var now = DateTime.Now;
            await couponRepository.InsertUserCouponAsync(new UserCoupon
            {
                UserId = record.UserId,
                CouponActivityId = couponActivity.Id,
                CouponTemplateId = couponActivity.CouponTemplateId,
                PlatformId = couponActivity.PlatformId,
                MerchantId = couponActivity.MerchantId,
                Status = (int)UserCouponStatus.Unused,
                Source = (int)UserCouponSource.Gift,
                ReceivedAt = now,
                ExpireAt = now.AddDays(template.ValidDays)
            });
            await activityRepository.UpdateRecordGiftCountAsync(record.Id, 1);
        }
    }

    /// <summary>订单取消/关单时回退券占用。</summary>
    public async Task<MarketingCommitResponse> ReleaseAsync(string orderNo, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(orderNo))
            return new MarketingCommitResponse { Success = false, Message = "缺少订单号" };
        await couponRepository.ReleaseUserCouponAsync(orderNo);
        return new MarketingCommitResponse { Success = true };
    }
}
