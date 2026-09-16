namespace MarketingService.Domain.Enums;

/// <summary>活动类型：满减/满折/满赠（赠券，通过券活动发放）。</summary>
public enum ActivityType
{
    /// <summary>满 X 元减 Y 元（DiscountValue 为减免金额）。</summary>
    FullReduce = 1,

    /// <summary>满 X 元打 Y 折（DiscountValue 为折扣率，0.85 表示 8.5 折）。</summary>
    FullDiscount = 2,

    /// <summary>满 X 元赠券（GiftCouponActivityId 指向实际发券的券活动）。</summary>
    GiftCoupon = 3
}

/// <summary>券模板类型：满减/满折/0元减（无门槛立减，抵扣后金额必须大于 0）。</summary>
public enum CouponType
{
    /// <summary>满 X 元减 Y 元（DiscountValue 为减免金额）。</summary>
    FullReduce = 1,

    /// <summary>满 X 元打 Y 折（DiscountValue 为折扣率，0.85 表示 8.5 折）。</summary>
    FullDiscount = 2,

    /// <summary>无门槛立减 Y 元（Threshold=0；抵扣金额必须小于商品行金额）。</summary>
    NoThresholdReduce = 3
}

/// <summary>
/// 活动/券活动的参与范围：平台活动支持全平台/指定商户/指定商户商品；商户活动支持全部商品/指定商品。
/// </summary>
public enum MarketingScopeType
{
    /// <summary>全部范围（平台活动=全平台；商户活动=本商户全部商品）。</summary>
    All = 1,

    /// <summary>指定商户（仅平台活动）。</summary>
    Merchant = 2,

    /// <summary>指定商户商品（TargetId 为 SKU，记录所属商户）。</summary>
    Products = 3
}

/// <summary>参与范围目标类型。</summary>
public enum MarketingTargetType
{
    Merchant = 1,
    Product = 2
}

/// <summary>券与活动的计算优先级（平台营销配置）。</summary>
public enum DiscountPriority
{
    /// <summary>活动优先：先取最优活动，无活动可用时才用券。</summary>
    ActivityFirst = 1,

    /// <summary>券优先：先取最优券，无券可用时才参加活动。</summary>
    CouponFirst = 2
}

/// <summary>用户券状态。</summary>
public enum UserCouponStatus
{
    Unused = 1,
    Used = 2,
    Expired = 3
}

/// <summary>用户券来源。</summary>
public enum UserCouponSource
{
    /// <summary>领券中心主动领取。</summary>
    Claim = 1,

    /// <summary>满赠活动自动发放。</summary>
    Gift = 2
}

/// <summary>单商品命中的营销类型（用于订单明细快照）。</summary>
public enum MarketingHitType
{
    None = 0,
    Activity = 1,
    Coupon = 2
}
