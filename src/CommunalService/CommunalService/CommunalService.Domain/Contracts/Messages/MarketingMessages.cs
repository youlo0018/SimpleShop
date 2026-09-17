using MessagePack;

namespace CommunalService.Domain.Contracts.Messages;

/// <summary>结算输入商品行（价格与数量以订单快照为准，营销服务只做优惠计算，不回查商品）。</summary>
[MessagePackObject]
public sealed class MarketingSettleItem
{
    /// <summary>商品 SKU ID；参与范围（指定商品）与优惠归属都按它匹配。</summary>
    [Key(0)] public long SkuId { get; set; }

    /// <summary>商品所属平台 ID；与活动/券所属平台必须一致才可用。</summary>
    [Key(1)] public long PlatformId { get; set; }

    /// <summary>商品所属商户 ID；商户活动/商户券按它匹配。</summary>
    [Key(2)] public long MerchantId { get; set; }

    /// <summary>商品名称快照，用于参与记录明细。</summary>
    [Key(3)] public string ProductName { get; set; } = string.Empty;

    /// <summary>商品单价（元），必须大于 0。</summary>
    [Key(4)] public decimal Price { get; set; }

    /// <summary>购买数量（1-99），与订单校验一致。</summary>
    [Key(5)] public int Quantity { get; set; }
}

/// <summary>
/// 结算请求：SelectedUserCouponIds 为 null 时自动使用全部可用券（购物车/提交页默认），
/// 传空数组表示"本单不使用券"（用户取消勾选）；SettleAsync 时 OrderNo 必填（用于占用与回退）。
/// </summary>
[MessagePackObject]
public sealed class MarketingSettleRequest
{
    /// <summary>下单平台 ID。</summary>
    [Key(0)] public long PlatformId { get; set; }

    /// <summary>下单用户 ID；为 0 时不加载用户券（仅活动可用）。</summary>
    [Key(1)] public long UserId { get; set; }

    /// <summary>订单 ID（预售/占用时未知可传 0，落账后以 Commit 的 OrderId 为准）。</summary>
    [Key(2)] public long OrderId { get; set; }

    /// <summary>订单号；Settle 必填，作为券占用与回退的关联键。</summary>
    [Key(3)] public string OrderNo { get; set; } = string.Empty;

    /// <summary>用户勾选使用的用户券 ID 列表；null=自动全部可用券，[]=不使用券。</summary>
    [Key(4)] public List<long>? SelectedUserCouponIds { get; set; }

    /// <summary>结算商品行（至少一行）。</summary>
    [Key(5)] public List<MarketingSettleItem> Items { get; set; } = [];
}

/// <summary>单商品优惠结果：每个商品最多命中活动或券之一（互斥）。</summary>
[MessagePackObject]
public sealed class MarketingSettleItemResult
{
    /// <summary>商品 SKU ID。</summary>
    [Key(0)] public long SkuId { get; set; }

    /// <summary>命中类型，取值见 <c>MarketingHitType</c>：0 无 / 1 活动 / 2 券。</summary>
    [Key(1)] public int HitType { get; set; }

    /// <summary>命中的活动 ID（HitType=1 时有效）。</summary>
    [Key(2)] public long ActivityId { get; set; }

    /// <summary>活动名称快照（订单明细展示用）。</summary>
    [Key(3)] public string ActivityName { get; set; } = string.Empty;

    /// <summary>活动类型快照：1 满减 / 2 满折 / 3 满赠。</summary>
    [Key(4)] public int ActivityType { get; set; }

    /// <summary>活动记录 ID（预留，当前落账时由营销侧生成）。</summary>
    [Key(5)] public long ActivityRecordId { get; set; }

    /// <summary>占用的用户券 ID（HitType=2 时有效）。</summary>
    [Key(6)] public long UserCouponId { get; set; }

    /// <summary>券活动 ID（HitType=2 时有效）。</summary>
    [Key(7)] public long CouponActivityId { get; set; }

    /// <summary>券名称快照（券活动名，缺失回退模板名）。</summary>
    [Key(8)] public string CouponName { get; set; } = string.Empty;

    /// <summary>本商品行优惠金额（元）；满赠固定为 0。</summary>
    [Key(9)] public decimal DiscountAmount { get; set; }

    /// <summary>商品行小计（原价，元）。</summary>
    [Key(10)] public decimal ItemAmount { get; set; }

    /// <summary>商品行实付金额（元）= ItemAmount - DiscountAmount，最小 0.01。</summary>
    [Key(11)] public decimal PayAmount { get; set; }

    /// <summary>满赠命中时实际发放的券活动 ID（ActivityType=3 时有效）。</summary>
    [Key(12)] public long GiftCouponActivityId { get; set; }

    /// <summary>商品名称快照（参与记录明细用）。</summary>
    [Key(13)] public string ProductName { get; set; } = string.Empty;

    /// <summary>购买数量（参与记录明细用）。</summary>
    [Key(14)] public int Quantity { get; set; }
}

/// <summary>结算结果（下单使用）：Success=false 时 Message 为可直接展示给用户的失败原因。</summary>
[MessagePackObject]
public sealed class MarketingSettleResponse
{
    /// <summary>是否结算成功；失败时不占用任何券。</summary>
    [Key(0)] public bool Success { get; set; }

    /// <summary>失败原因（成功时为空串）。</summary>
    [Key(1)] public string Message { get; set; } = string.Empty;

    /// <summary>活动折扣合计（元）。</summary>
    [Key(2)] public decimal ActivityDiscount { get; set; }

    /// <summary>券抵扣合计（元）。</summary>
    [Key(3)] public decimal CouponDiscount { get; set; }

    /// <summary>总优惠（元）= ActivityDiscount + CouponDiscount。</summary>
    [Key(4)] public decimal TotalDiscount { get; set; }

    /// <summary>逐商品优惠结果（与请求行一一对应）。</summary>
    [Key(5)] public List<MarketingSettleItemResult> Items { get; set; } = [];
}

/// <summary>可用券展示项（购物车/提交页的券列表）。</summary>
[MessagePackObject]
public sealed class MarketingAvailableCoupon
{
    /// <summary>用户券 ID（前端勾选与下单回传都用它）。</summary>
    [Key(0)] public long UserCouponId { get; set; }

    /// <summary>券活动 ID。</summary>
    [Key(1)] public long CouponActivityId { get; set; }

    /// <summary>券名称（券活动名，缺失回退模板名）。</summary>
    [Key(2)] public string Name { get; set; } = string.Empty;

    /// <summary>券类型：1 满减 / 2 满折 / 3 0元减。</summary>
    [Key(3)] public int CouponType { get; set; }

    /// <summary>门槛金额（元）。</summary>
    [Key(4)] public decimal Threshold { get; set; }

    /// <summary>优惠值：满减/0元减=金额，满折=折扣率。</summary>
    [Key(5)] public decimal DiscountValue { get; set; }

    /// <summary>券过期时间。</summary>
    [Key(6)] public DateTime ExpireAt { get; set; }

    /// <summary>按当前购物车自动结算时该券可带来的优惠（0=当前不适用，前端据此过滤展示）。</summary>
    [Key(7)] public decimal EstimatedDiscount { get; set; }
}

/// <summary>可用活动展示项（购物车/提交页的活动列表）。</summary>
[MessagePackObject]
public sealed class MarketingAvailableActivity
{
    /// <summary>活动 ID。</summary>
    [Key(0)] public long ActivityId { get; set; }

    /// <summary>活动名称。</summary>
    [Key(1)] public string Name { get; set; } = string.Empty;

    /// <summary>活动类型：1 满减 / 2 满折 / 3 满赠。</summary>
    [Key(2)] public int ActivityType { get; set; }

    /// <summary>门槛金额（元）。</summary>
    [Key(3)] public decimal Threshold { get; set; }

    /// <summary>优惠值：满减=金额，满折=折扣率，满赠=0。</summary>
    [Key(4)] public decimal DiscountValue { get; set; }

    /// <summary>当前购物车下的预估优惠（满赠为 0，前端展示为"赠券"）。</summary>
    [Key(5)] public decimal EstimatedDiscount { get; set; }
}

/// <summary>结算预览结果：逐商品优惠 + 可用券/活动列表（前端展示浮动金额，不产生任何副作用）。</summary>
[MessagePackObject]
public sealed class MarketingPreviewResponse
{
    /// <summary>是否成功（失败时 Message 为原因）。</summary>
    [Key(0)] public bool Success { get; set; }

    /// <summary>失败原因（成功时为空串）。</summary>
    [Key(1)] public string Message { get; set; } = string.Empty;

    /// <summary>活动折扣合计（元）。</summary>
    [Key(2)] public decimal ActivityDiscount { get; set; }

    /// <summary>券抵扣合计（元）。</summary>
    [Key(3)] public decimal CouponDiscount { get; set; }

    /// <summary>总优惠（元）。</summary>
    [Key(4)] public decimal TotalDiscount { get; set; }

    /// <summary>逐商品优惠结果。</summary>
    [Key(5)] public List<MarketingSettleItemResult> Items { get; set; } = [];

    /// <summary>可用券列表（按预估优惠降序，预估为 0 的不返回）。</summary>
    [Key(6)] public List<MarketingAvailableCoupon> Coupons { get; set; } = [];

    /// <summary>可用活动列表（按预估优惠降序）。</summary>
    [Key(7)] public List<MarketingAvailableActivity> Activities { get; set; } = [];
}

/// <summary>落记录请求：订单落库后携带结算结果提交，营销服务不重算（订单号幂等）。</summary>
[MessagePackObject]
public sealed class MarketingCommitRequest
{
    /// <summary>订单主键 ID（雪花，落账后已知）。</summary>
    [Key(0)] public long OrderId { get; set; }

    /// <summary>订单号（幂等键）。</summary>
    [Key(1)] public string OrderNo { get; set; } = string.Empty;

    /// <summary>下单用户 ID。</summary>
    [Key(2)] public long UserId { get; set; }

    /// <summary>平台 ID。</summary>
    [Key(3)] public long PlatformId { get; set; }

    /// <summary>SettleAsync 返回的完整结算结果（逐商品命中信息与优惠金额）。</summary>
    [Key(4)] public MarketingSettleResponse Settle { get; set; } = new();
}

/// <summary>回退请求：订单创建失败/取消时释放该订单占用的券。</summary>
[MessagePackObject]
public sealed class MarketingReleaseRequest
{
    /// <summary>订单号（回退依据：占用时记录的 UsedOrderNo）。</summary>
    [Key(0)] public string OrderNo { get; set; } = string.Empty;
}

/// <summary>落账/回退的简单结果。</summary>
[MessagePackObject]
public sealed class MarketingCommitResponse
{
    /// <summary>是否成功。</summary>
    [Key(0)] public bool Success { get; set; }

    /// <summary>失败原因（成功时为空串）。</summary>
    [Key(1)] public string Message { get; set; } = string.Empty;
}
