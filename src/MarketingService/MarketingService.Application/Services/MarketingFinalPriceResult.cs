namespace MarketingService.Application.Services;

/// <summary>
/// 到手价试算结果（京东/淘宝式价格拆分）：到手价 = 原价小计 - 活动优惠 - 券优惠，
/// 前端据此展示"到手价 + 划线原价 + 优惠来源标签"，不自行拼算金额。
/// </summary>
public sealed class MarketingFinalPriceResult
{
    /// <summary>商品 SKU ID。</summary>
    public long SkuId { get; init; }

    /// <summary>原价小计（元）= 单价 × 数量。</summary>
    public decimal OriginalPrice { get; init; }

    /// <summary>活动优惠金额（元）；未命中活动为 0。</summary>
    public decimal ActivityDiscount { get; init; }

    /// <summary>券优惠金额（元）；未命中券为 0。</summary>
    public decimal CouponDiscount { get; init; }

    /// <summary>到手价（元）= 原价 - 活动 - 券，最低 0.01。</summary>
    public decimal FinalPrice { get; init; }

    /// <summary>命中类型：0 无 / 1 活动 / 2 券（与结算引擎一致）。</summary>
    public int HitType { get; init; }

    /// <summary>命中的活动 ID（HitType=1 时有效）。</summary>
    public long ActivityId { get; init; }

    /// <summary>活动名称快照（展示"满减/满折"来源）。</summary>
    public string ActivityName { get; init; } = string.Empty;

    /// <summary>券名称快照（展示"券后"来源）。</summary>
    public string CouponName { get; init; } = string.Empty;

    /// <summary>满赠活动 ID（HitType=1 且活动类型为满赠时有效，此时优惠为 0）。</summary>
    public long GiftCouponActivityId { get; init; }
}
