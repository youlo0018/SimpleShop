using MessagePack;

namespace CommunalService.Domain.Contracts.Messages;

/// <summary>结算输入商品行（价格与数量以订单快照为准，营销服务只做优惠计算）。</summary>
[MessagePackObject]
public sealed class MarketingSettleItem
{
    [Key(0)] public long SkuId { get; set; }
    [Key(1)] public long PlatformId { get; set; }
    [Key(2)] public long MerchantId { get; set; }
    [Key(3)] public string ProductName { get; set; } = string.Empty;
    [Key(4)] public decimal Price { get; set; }
    [Key(5)] public int Quantity { get; set; }
}

/// <summary>
/// 结算请求：SelectedUserCouponIds 为 null 时自动使用全部可用券（购物车预览），
/// 传空数组表示"本单不使用券"（提交页用户取消勾选）。
/// </summary>
[MessagePackObject]
public sealed class MarketingSettleRequest
{
    [Key(0)] public long PlatformId { get; set; }
    [Key(1)] public long UserId { get; set; }
    [Key(2)] public long OrderId { get; set; }
    [Key(3)] public string OrderNo { get; set; } = string.Empty;
    [Key(4)] public List<long>? SelectedUserCouponIds { get; set; }
    [Key(5)] public List<MarketingSettleItem> Items { get; set; } = [];
}

/// <summary>单商品优惠结果：命中活动或券，二者互斥。</summary>
[MessagePackObject]
public sealed class MarketingSettleItemResult
{
    [Key(0)] public long SkuId { get; set; }

    /// <summary>0=无 1=活动 2=券（MarketingHitType）。</summary>
    [Key(1)] public int HitType { get; set; }

    [Key(2)] public long ActivityId { get; set; }
    [Key(3)] public string ActivityName { get; set; } = string.Empty;
    [Key(4)] public int ActivityType { get; set; }
    [Key(5)] public long ActivityRecordId { get; set; }

    [Key(6)] public long UserCouponId { get; set; }
    [Key(7)] public long CouponActivityId { get; set; }
    [Key(8)] public string CouponName { get; set; } = string.Empty;

    [Key(9)] public decimal DiscountAmount { get; set; }
    [Key(10)] public decimal ItemAmount { get; set; }
    [Key(11)] public decimal PayAmount { get; set; }

    /// <summary>满赠命中时实际发放的券活动。</summary>
    [Key(12)] public long GiftCouponActivityId { get; set; }

    [Key(13)] public string ProductName { get; set; } = string.Empty;
    [Key(14)] public int Quantity { get; set; }
}

/// <summary>结算结果（下单使用）。</summary>
[MessagePackObject]
public sealed class MarketingSettleResponse
{
    [Key(0)] public bool Success { get; set; }
    [Key(1)] public string Message { get; set; } = string.Empty;
    [Key(2)] public decimal ActivityDiscount { get; set; }
    [Key(3)] public decimal CouponDiscount { get; set; }
    [Key(4)] public decimal TotalDiscount { get; set; }
    [Key(5)] public List<MarketingSettleItemResult> Items { get; set; } = [];
}

/// <summary>可用券展示项（购物车/提交页）。</summary>
[MessagePackObject]
public sealed class MarketingAvailableCoupon
{
    [Key(0)] public long UserCouponId { get; set; }
    [Key(1)] public long CouponActivityId { get; set; }
    [Key(2)] public string Name { get; set; } = string.Empty;
    [Key(3)] public int CouponType { get; set; }
    [Key(4)] public decimal Threshold { get; set; }
    [Key(5)] public decimal DiscountValue { get; set; }
    [Key(6)] public DateTime ExpireAt { get; set; }

    /// <summary>按当前购物车自动结算时该券能带来的优惠（0=当前不适用）。</summary>
    [Key(7)] public decimal EstimatedDiscount { get; set; }
}

/// <summary>可用活动展示项。</summary>
[MessagePackObject]
public sealed class MarketingAvailableActivity
{
    [Key(0)] public long ActivityId { get; set; }
    [Key(1)] public string Name { get; set; } = string.Empty;
    [Key(2)] public int ActivityType { get; set; }
    [Key(3)] public decimal Threshold { get; set; }
    [Key(4)] public decimal DiscountValue { get; set; }
    [Key(5)] public decimal EstimatedDiscount { get; set; }
}

/// <summary>结算预览结果：逐商品优惠 + 可用券/活动列表（前端展示浮动金额）。</summary>
[MessagePackObject]
public sealed class MarketingPreviewResponse
{
    [Key(0)] public bool Success { get; set; }
    [Key(1)] public string Message { get; set; } = string.Empty;
    [Key(2)] public decimal ActivityDiscount { get; set; }
    [Key(3)] public decimal CouponDiscount { get; set; }
    [Key(4)] public decimal TotalDiscount { get; set; }
    [Key(5)] public List<MarketingSettleItemResult> Items { get; set; } = [];
    [Key(6)] public List<MarketingAvailableCoupon> Coupons { get; set; } = [];
    [Key(7)] public List<MarketingAvailableActivity> Activities { get; set; } = [];
}

/// <summary>落记录请求：订单落库后携带结算结果提交，服务端不再重算。</summary>
[MessagePackObject]
public sealed class MarketingCommitRequest
{
    [Key(0)] public long OrderId { get; set; }
    [Key(1)] public string OrderNo { get; set; } = string.Empty;
    [Key(2)] public long UserId { get; set; }
    [Key(3)] public long PlatformId { get; set; }
    [Key(4)] public MarketingSettleResponse Settle { get; set; } = new();
}

[MessagePackObject]
public sealed class MarketingReleaseRequest
{
    [Key(0)] public string OrderNo { get; set; } = string.Empty;
}

[MessagePackObject]
public sealed class MarketingCommitResponse
{
    [Key(0)] public bool Success { get; set; }
    [Key(1)] public string Message { get; set; } = string.Empty;
}
