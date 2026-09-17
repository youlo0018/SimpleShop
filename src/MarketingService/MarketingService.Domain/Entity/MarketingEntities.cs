using System.ComponentModel;
using CommunalService.Domain.Entity;
using FreeSql.DataAnnotations;
using MarketingService.Domain.Enums;

namespace MarketingService.Domain.Entity;

/// <summary>
/// 营销活动：平台活动（MerchantId=0）或商户活动，参与范围由 marketing_activity_target 描述。
/// 参与判定与优惠计算在 DiscountEngine，订单快照写在 order/order_item。
/// </summary>
[Table(Name = "marketing_activity")]
[Index("idx_activity_platform", "PlatformId, IsEnabled")]
public sealed class MarketingActivity : BaseEntity
{
    /// <summary>活动所属平台 ID；跨平台不可见、不可用。</summary>
    [Description("所属平台")] public long PlatformId { get; set; }

    /// <summary>活动所属商户 ID；0 表示平台活动（全平台/指定商户/指定商品），大于 0 表示商户活动（本商户全部/指定商品）。</summary>
    [Description("所属商户，0=平台活动")] public long MerchantId { get; set; }

    /// <summary>活动名称，最长 64 字符；后台列表与订单快照都使用它。</summary>
    [Column(StringLength = 64), Description("活动名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>活动说明，最长 255 字符，仅后台展示，不参与计算。</summary>
    [Column(StringLength = 255), Description("活动说明")]
    public string Description { get; set; } = string.Empty;

    /// <summary>活动类型，取值见 <see cref="ActivityType"/>：1 满减 / 2 满折 / 3 满赠。</summary>
    [Description("类型 1满减 2满折 3满赠")] public int ActivityType { get; set; }

    /// <summary>门槛金额（元）：按商品行小计判断，达到才可参与；满赠同样需要满足门槛。</summary>
    [Description("门槛金额（按商品行小计）")] public decimal Threshold { get; set; }

    /// <summary>
    /// 优惠值：满减=减免金额（元）；满折=折扣率（0.85 表示 8.5 折）；满赠固定为 0（赠品由 GiftCouponActivityId 决定）。
    /// </summary>
    [Description("满减=减免金额；满折=折扣率(0-1)；满赠=0")] public decimal DiscountValue { get; set; }

    /// <summary>满赠活动命中后向用户发放的券活动 ID；非满赠活动必须为 0。</summary>
    [Description("满赠发放的券活动ID")] public long GiftCouponActivityId { get; set; }

    /// <summary>参与范围，取值见 <see cref="MarketingScopeType"/>：1 全部 / 2 指定商户 / 3 指定商户商品。</summary>
    [Description("范围 1全部 2指定商户 3指定商品")] public int ScopeType { get; set; } = (int)MarketingScopeType.All;

    /// <summary>活动开始时间（含），早于该时间不参与计算。</summary>
    [Description("开始时间")] public DateTime StartAt { get; set; } = DateTime.Now;

    /// <summary>活动结束时间（不含）；为空表示长期有效。</summary>
    [Column(IsNullable = true), Description("结束时间，空=长期")]
    public DateTime? EndAt { get; set; }

    /// <summary>是否启用；停用后立即不参与新的优惠计算，已产生的订单记录不受影响。</summary>
    [Description("是否启用")] public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// 活动参与范围明细：TargetType=1 时 TargetId 为商户 ID；TargetType=2 时为 SKU ID（并用 MerchantId 冗余所属商户便于过滤）。
/// 名单在保存活动时整组替换（旧数据软删）。
/// </summary>
[Table(Name = "marketing_activity_target")]
[Index("idx_activity_target_activity", "ActivityId")]
public sealed class MarketingActivityTarget : BaseEntity
{
    /// <summary>所属活动 ID。</summary>
    public long ActivityId { get; set; }

    /// <summary>目标类型，取值见 <see cref="MarketingTargetType"/>：1 商户 / 2 商品（SKU）。</summary>
    [Description("1商户 2商品")] public int TargetType { get; set; }

    /// <summary>目标 ID：TargetType=1 时为商户 ID，=2 时为 SKU ID。</summary>
    [Description("商户ID或SKU ID")] public long TargetId { get; set; }

    /// <summary>商品所属商户（TargetType=2 时冗余存储，用于快速过滤）。</summary>
    [Description("商品所属商户（TargetType=2 时用于快速过滤）")] public long MerchantId { get; set; }
}

/// <summary>
/// 券模板：仅定义优惠规则与领取后有效期，实际发券与使用范围由券活动（coupon_activity）决定。
/// 模板修改不影响已发放的用户券（用户券已快照 TemplateId，使用时读取最新模板规则）。
/// </summary>
[Table(Name = "coupon_template")]
[Index("idx_coupon_template_platform", "PlatformId, IsEnabled")]
public sealed class CouponTemplate : BaseEntity
{
    /// <summary>模板所属平台 ID。</summary>
    [Description("所属平台")] public long PlatformId { get; set; }

    /// <summary>模板所属商户 ID；0 表示平台券模板。</summary>
    [Description("所属商户，0=平台券模板")] public long MerchantId { get; set; }

    /// <summary>模板名称，最长 64 字符；券活动与券包展示优先用券活动名，缺失时回退模板名。</summary>
    [Column(StringLength = 64), Description("模板名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>使用说明，最长 255 字符，仅展示。</summary>
    [Column(StringLength = 255), Description("使用说明")]
    public string Description { get; set; } = string.Empty;

    /// <summary>券类型，取值见 <see cref="CouponType"/>：1 满减 / 2 满折 / 3 0元减。</summary>
    [Description("类型 1满减 2满折 3零元减")] public int CouponType { get; set; }

    /// <summary>门槛金额（元）：满减/满折需商品行小计达到；0元减固定为 0。</summary>
    [Description("门槛金额；0元减固定为0")] public decimal Threshold { get; set; }

    /// <summary>优惠值：满减/0元减=优惠金额（元，必须小于商品行金额）；满折=折扣率（0.85 表示 8.5 折）。</summary>
    [Description("满减/0元减=金额；满折=折扣率(0-1)")] public decimal DiscountValue { get; set; }

    /// <summary>领取后有效天数（1-365），用户券过期时间 = 领取时间 + ValidDays。</summary>
    [Description("领取后有效天数")] public int ValidDays { get; set; } = 7;

    /// <summary>是否启用；停用后不可领取/发放，已领取的券仍可使用到过期（使用侧只认用户券状态）。</summary>
    [Description("是否启用")] public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// 券活动：券的实际发放与使用范围载体；领券中心与满赠都通过券活动发券。
/// 发行量用条件自增（IssuedCount &lt; TotalStock）防超发。
/// </summary>
[Table(Name = "coupon_activity")]
[Index("idx_coupon_activity_platform", "PlatformId, IsEnabled")]
public sealed class CouponActivity : BaseEntity
{
    /// <summary>券活动所属平台 ID。</summary>
    [Description("所属平台")] public long PlatformId { get; set; }

    /// <summary>券活动所属商户 ID；0 表示平台券活动。</summary>
    [Description("所属商户，0=平台券活动")] public long MerchantId { get; set; }

    /// <summary>券活动名称（展示名），最长 64 字符。</summary>
    [Column(StringLength = 64), Description("券活动名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>使用的券模板 ID；优惠规则以模板为准。</summary>
    public long CouponTemplateId { get; set; }

    /// <summary>使用范围，取值同 <see cref="MarketingScopeType"/>：1 全部 / 2 指定商户 / 3 指定商户商品。</summary>
    [Description("范围 1全部 2指定商户 3指定商品")] public int ScopeType { get; set; } = (int)MarketingScopeType.All;

    /// <summary>发行总量，达到后不可再发放（领取与满赠共用该库存）。</summary>
    [Description("发行总量")] public int TotalStock { get; set; } = 1000;

    /// <summary>已发放数量（含领取与满赠），条件自增保证不超发。</summary>
    [Description("已发放数量")] public int IssuedCount { get; set; }

    /// <summary>每人限领数量（仅约束领券中心主动领取；满赠发放不受此限制）。</summary>
    [Description("每人限领数量")] public int PerUserLimit { get; set; } = 1;

    /// <summary>是否在领券中心展示并可领取；满赠专用券活动可设为 false，只由活动触发发放。</summary>
    [Description("是否可在领券中心领取")] public bool IsClaimable { get; set; } = true;

    /// <summary>发放开始时间（含）。</summary>
    [Description("开始时间")] public DateTime StartAt { get; set; } = DateTime.Now;

    /// <summary>发放结束时间（不含）；为空表示长期有效。</summary>
    [Column(IsNullable = true), Description("结束时间，空=长期")]
    public DateTime? EndAt { get; set; }

    /// <summary>是否启用；停用后不再发放，已发用户券仍按用户券状态使用。</summary>
    [Description("是否启用")] public bool IsEnabled { get; set; } = true;
}

/// <summary>券活动参与范围明细，语义与 <see cref="MarketingActivityTarget"/> 完全一致。</summary>
[Table(Name = "coupon_activity_target")]
[Index("idx_coupon_activity_target_activity", "CouponActivityId")]
public sealed class CouponActivityTarget : BaseEntity
{
    /// <summary>所属券活动 ID。</summary>
    public long CouponActivityId { get; set; }

    /// <summary>目标类型：1 商户 / 2 商品（SKU）。</summary>
    [Description("1商户 2商品")] public int TargetType { get; set; }

    /// <summary>目标 ID：商户 ID 或 SKU ID。</summary>
    public long TargetId { get; set; }

    /// <summary>商品所属商户（TargetType=2 时冗余）。</summary>
    public long MerchantId { get; set; }
}

/// <summary>
/// 用户券实例：领取/满赠发放后进入券包；使用时条件更新为 Used（一张券只能被一单占用），
/// 订单取消/关单时回退为 Unused；过期由查询时按 ExpireAt 判定（不落库改状态）。
/// </summary>
[Table(Name = "user_coupon")]
[Index("idx_user_coupon_user", "UserId, Status")]
public sealed class UserCoupon : BaseEntity
{
    /// <summary>券持有人用户 ID。</summary>
    public long UserId { get; set; }

    /// <summary>发放该券的券活动 ID（券包展示与使用范围判定都用它）。</summary>
    public long CouponActivityId { get; set; }

    /// <summary>券模板 ID（优惠规则来源）。</summary>
    public long CouponTemplateId { get; set; }

    /// <summary>券所属平台 ID（下单商品必须同平台才可用）。</summary>
    public long PlatformId { get; set; }

    /// <summary>券所属商户 ID；0=平台券，仅可抵扣同平台商品。</summary>
    [Description("券所属商户，0=平台券")] public long MerchantId { get; set; }

    /// <summary>券状态，取值见 <see cref="UserCouponStatus"/>：1 未使用 / 2 已使用 / 3 已过期。</summary>
    [Description("1未使用 2已使用 3已过期")] public int Status { get; set; } = (int)UserCouponStatus.Unused;

    /// <summary>来源，取值见 <see cref="UserCouponSource"/>：1 领券中心领取 / 2 满赠发放。</summary>
    [Description("来源 1领取 2满赠")] public int Source { get; set; } = (int)UserCouponSource.Claim;

    /// <summary>领取/发放时间。</summary>
    public DateTime ReceivedAt { get; set; } = DateTime.Now;

    /// <summary>过期时间 = ReceivedAt + 模板 ValidDays；查询时严格大于当前时间才可用。</summary>
    public DateTime ExpireAt { get; set; }

    /// <summary>使用时间；未使用时为空。</summary>
    [Column(IsNullable = true), Description("使用时间")]
    public DateTime? UsedAt { get; set; }

    /// <summary>占用该券的订单号；回退与对账都依赖它，未使用时为空。</summary>
    [Column(StringLength = 32, IsNullable = true), Description("使用的订单号")]
    public string? UsedOrderNo { get; set; }
}

/// <summary>活动参与记录（订单级）：每个参与活动的订单一条，供效果报表与订单溯源。</summary>
[Table(Name = "marketing_activity_record")]
[Index("idx_activity_record_activity", "ActivityId")]
[Index("idx_activity_record_order", "OrderNo")]
public sealed class MarketingActivityRecord : BaseEntity
{
    /// <summary>参与的活动 ID。</summary>
    public long ActivityId { get; set; }

    /// <summary>活动名称快照（活动改名后报表仍显示成交时名称）。</summary>
    [Column(StringLength = 64), Description("活动名称快照")]
    public string ActivityName { get; set; } = string.Empty;

    /// <summary>活动类型快照，取值见 <see cref="ActivityType"/>；满赠记录折扣为 0。</summary>
    [Description("活动类型快照 1满减 2满折 3满赠")] public int ActivityType { get; set; }

    /// <summary>活动所属平台 ID（报表租户过滤条件）。</summary>
    public long PlatformId { get; set; }

    /// <summary>活动所属商户 ID；0=平台活动。</summary>
    public long MerchantId { get; set; }

    /// <summary>订单主键 ID（雪花，报表下钻与对账用）。</summary>
    public long OrderId { get; set; }

    /// <summary>订单号快照（幂等键：同一订单不重复落账）。</summary>
    [Column(StringLength = 32), Description("订单号")]
    public string OrderNo { get; set; } = string.Empty;

    /// <summary>下单用户 ID。</summary>
    public long UserId { get; set; }

    /// <summary>该订单在本活动产生的折扣合计（元）。</summary>
    [Description("该订单本活动产生的折扣合计")] public decimal DiscountAmount { get; set; }

    /// <summary>满赠实际发放的券张数；支付成功后才写入（0 表示未发放或非满赠）。</summary>
    [Description("满赠发放的券张数")] public int GiftCouponCount { get; set; }
}

/// <summary>活动参与记录（商品行级）：每商品每活动一条，报表按商品维度统计。</summary>
[Table(Name = "marketing_activity_record_item")]
[Index("idx_activity_record_item_record", "RecordId")]
public sealed class MarketingActivityRecordItem : BaseEntity
{
    /// <summary>所属订单级记录 ID。</summary>
    public long RecordId { get; set; }

    /// <summary>活动 ID（冗余，便于按活动聚合）。</summary>
    public long ActivityId { get; set; }

    /// <summary>订单号（冗余，便于按订单检索明细）。</summary>
    [Column(StringLength = 32), Description("订单号，便于按订单检索")] public string OrderNo { get; set; } = string.Empty;

    /// <summary>商品 SKU ID。</summary>
    public long SkuId { get; set; }

    /// <summary>商品名称快照。</summary>
    [Column(StringLength = 128), Description("商品名称快照")]
    public string ProductName { get; set; } = string.Empty;

    /// <summary>购买数量。</summary>
    public int Quantity { get; set; }

    /// <summary>商品行小计（原价 = 单价×数量，元）。</summary>
    [Description("商品行小计（原价）")] public decimal ItemAmount { get; set; }

    /// <summary>本商品行在本活动下的优惠金额（元）。</summary>
    [Description("商品行优惠金额")] public decimal DiscountAmount { get; set; }
}

/// <summary>券使用记录（订单级）：每券每订单一条，供券效果报表。</summary>
[Table(Name = "coupon_record")]
[Index("idx_coupon_record_activity", "CouponActivityId")]
[Index("idx_coupon_record_order", "OrderNo")]
public sealed class CouponRecord : BaseEntity
{
    /// <summary>使用的券活动 ID。</summary>
    public long CouponActivityId { get; set; }

    /// <summary>券活动名称快照。</summary>
    [Column(StringLength = 64), Description("券活动名称快照")]
    public string CouponActivityName { get; set; } = string.Empty;

    /// <summary>券模板 ID。</summary>
    public long CouponTemplateId { get; set; }

    /// <summary>被占用的用户券 ID（一张券一单一次）。</summary>
    public long UserCouponId { get; set; }

    /// <summary>券所属平台 ID。</summary>
    public long PlatformId { get; set; }

    /// <summary>券所属商户 ID；0=平台券。</summary>
    public long MerchantId { get; set; }

    /// <summary>订单主键 ID。</summary>
    public long OrderId { get; set; }

    /// <summary>订单号快照（幂等键）。</summary>
    [Column(StringLength = 32), Description("订单号")]
    public string OrderNo { get; set; } = string.Empty;

    /// <summary>下单用户 ID。</summary>
    public long UserId { get; set; }

    /// <summary>本券在本订单的抵扣合计（元）。</summary>
    [Description("本券在本订单的抵扣合计")] public decimal DiscountAmount { get; set; }
}

/// <summary>券使用记录（商品行级）：每券每商品行一条。</summary>
[Table(Name = "coupon_record_item")]
[Index("idx_coupon_record_item_record", "RecordId")]
public sealed class CouponRecordItem : BaseEntity
{
    /// <summary>所属订单级记录 ID。</summary>
    public long RecordId { get; set; }

    /// <summary>券活动 ID（冗余）。</summary>
    public long CouponActivityId { get; set; }

    /// <summary>订单号（冗余，便于按订单检索）。</summary>
    [Column(StringLength = 32), Description("订单号")] public string OrderNo { get; set; } = string.Empty;

    /// <summary>商品 SKU ID。</summary>
    public long SkuId { get; set; }

    /// <summary>商品名称快照。</summary>
    [Column(StringLength = 128), Description("商品名称快照")]
    public string ProductName { get; set; } = string.Empty;

    /// <summary>购买数量。</summary>
    public int Quantity { get; set; }

    /// <summary>商品行小计（原价，元）。</summary>
    public decimal ItemAmount { get; set; }

    /// <summary>本券在本商品行的抵扣金额（元）。</summary>
    public decimal DiscountAmount { get; set; }
}

/// <summary>
/// 平台营销配置：券/活动计算优先级（每个平台一条，缺省券优先）。
/// 由 DiscountEngine 每次结算读取；缺失时按 <see cref="DiscountPriority.CouponFirst"/> 处理。
/// </summary>
[Table(Name = "marketing_config")]
[Index("uk_marketing_config_platform", "PlatformId", IsUnique = true)]
public sealed class MarketingConfig : BaseEntity
{
    /// <summary>平台 ID（唯一，一个平台一条配置）。</summary>
    public long PlatformId { get; set; }

    /// <summary>优惠计算优先级，取值见 <see cref="DiscountPriority"/>：1 活动优先 / 2 券优先。</summary>
    [Description("1活动优先 2券优先")] public int DiscountPriority { get; set; } = (int)Enums.DiscountPriority.CouponFirst;
}
