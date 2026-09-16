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
    [Description("所属平台")] public long PlatformId { get; set; }

    [Description("所属商户，0=平台活动")] public long MerchantId { get; set; }

    [Column(StringLength = 64), Description("活动名称")]
    public string Name { get; set; } = string.Empty;

    [Column(StringLength = 255), Description("活动说明")]
    public string Description { get; set; } = string.Empty;

    [Description("类型 1满减 2满折 3满赠")] public int ActivityType { get; set; }

    [Description("门槛金额（按商品行小计）")] public decimal Threshold { get; set; }

    [Description("满减=减免金额；满折=折扣率(0-1)；满赠=0")] public decimal DiscountValue { get; set; }

    [Description("满赠发放的券活动ID")] public long GiftCouponActivityId { get; set; }

    [Description("范围 1全部 2指定商户 3指定商品")] public int ScopeType { get; set; } = (int)MarketingScopeType.All;

    [Description("开始时间")] public DateTime StartAt { get; set; } = DateTime.Now;

    [Column(IsNullable = true), Description("结束时间，空=长期")]
    public DateTime? EndAt { get; set; }

    [Description("是否启用")] public bool IsEnabled { get; set; } = true;
}

/// <summary>活动参与范围明细：TargetType=1 时 TargetId 为商户；=2 时为 SKU。</summary>
[Table(Name = "marketing_activity_target")]
[Index("idx_activity_target_activity", "ActivityId")]
public sealed class MarketingActivityTarget : BaseEntity
{
    public long ActivityId { get; set; }

    [Description("1商户 2商品")] public int TargetType { get; set; }

    [Description("商户ID或SKU ID")] public long TargetId { get; set; }

    [Description("商品所属商户（TargetType=2 时用于快速过滤）")] public long MerchantId { get; set; }
}

/// <summary>
/// 券模板：仅定义优惠规则与有效期，实际发券与使用范围由券活动（coupon_activity）决定。
/// </summary>
[Table(Name = "coupon_template")]
[Index("idx_coupon_template_platform", "PlatformId, IsEnabled")]
public sealed class CouponTemplate : BaseEntity
{
    [Description("所属平台")] public long PlatformId { get; set; }

    [Description("所属商户，0=平台券模板")] public long MerchantId { get; set; }

    [Column(StringLength = 64), Description("模板名称")]
    public string Name { get; set; } = string.Empty;

    [Column(StringLength = 255), Description("使用说明")]
    public string Description { get; set; } = string.Empty;

    [Description("类型 1满减 2满折 3零元减")] public int CouponType { get; set; }

    [Description("门槛金额；0元减固定为0")] public decimal Threshold { get; set; }

    [Description("满减/0元减=金额；满折=折扣率(0-1)")] public decimal DiscountValue { get; set; }

    [Description("领取后有效天数")] public int ValidDays { get; set; } = 7;

    [Description("是否启用")] public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// 券活动：券的实际发放与使用范围载体；领券中心与满赠都通过券活动发券。
/// </summary>
[Table(Name = "coupon_activity")]
[Index("idx_coupon_activity_platform", "PlatformId, IsEnabled")]
public sealed class CouponActivity : BaseEntity
{
    [Description("所属平台")] public long PlatformId { get; set; }

    [Description("所属商户，0=平台券活动")] public long MerchantId { get; set; }

    [Column(StringLength = 64), Description("券活动名称")]
    public string Name { get; set; } = string.Empty;

    public long CouponTemplateId { get; set; }

    [Description("范围 1全部 2指定商户 3指定商品")] public int ScopeType { get; set; } = (int)MarketingScopeType.All;

    [Description("发行总量")] public int TotalStock { get; set; } = 1000;

    [Description("已发放数量")] public int IssuedCount { get; set; }

    [Description("每人限领数量")] public int PerUserLimit { get; set; } = 1;

    [Description("是否可在领券中心领取")] public bool IsClaimable { get; set; } = true;

    [Description("开始时间")] public DateTime StartAt { get; set; } = DateTime.Now;

    [Column(IsNullable = true), Description("结束时间，空=长期")]
    public DateTime? EndAt { get; set; }

    [Description("是否启用")] public bool IsEnabled { get; set; } = true;
}

/// <summary>券活动参与范围明细，语义同 MarketingActivityTarget。</summary>
[Table(Name = "coupon_activity_target")]
[Index("idx_coupon_activity_target_activity", "CouponActivityId")]
public sealed class CouponActivityTarget : BaseEntity
{
    public long CouponActivityId { get; set; }

    [Description("1商户 2商品")] public int TargetType { get; set; }

    public long TargetId { get; set; }

    public long MerchantId { get; set; }
}

/// <summary>
/// 用户券实例：领取/满赠发放后进入券包，使用后置 Used，过期由查询时按 ExpireAt 判定。
/// </summary>
[Table(Name = "user_coupon")]
[Index("idx_user_coupon_user", "UserId, Status")]
public sealed class UserCoupon : BaseEntity
{
    public long UserId { get; set; }

    public long CouponActivityId { get; set; }

    public long CouponTemplateId { get; set; }

    public long PlatformId { get; set; }

    [Description("券所属商户，0=平台券")] public long MerchantId { get; set; }

    [Description("1未使用 2已使用 3已过期")] public int Status { get; set; } = (int)UserCouponStatus.Unused;

    [Description("来源 1领取 2满赠")] public int Source { get; set; } = (int)UserCouponSource.Claim;

    public DateTime ReceivedAt { get; set; } = DateTime.Now;

    public DateTime ExpireAt { get; set; }

    [Column(IsNullable = true), Description("使用时间")]
    public DateTime? UsedAt { get; set; }

    [Column(StringLength = 32, IsNullable = true), Description("使用的订单号")]
    public string? UsedOrderNo { get; set; }
}

/// <summary>活动参与记录（订单级）：每个参与活动的订单一条，供效果报表与订单溯源。</summary>
[Table(Name = "marketing_activity_record")]
[Index("idx_activity_record_activity", "ActivityId")]
[Index("idx_activity_record_order", "OrderNo")]
public sealed class MarketingActivityRecord : BaseEntity
{
    public long ActivityId { get; set; }

    [Column(StringLength = 64), Description("活动名称快照")]
    public string ActivityName { get; set; } = string.Empty;

    [Description("活动类型快照 1满减 2满折 3满赠")] public int ActivityType { get; set; }

    public long PlatformId { get; set; }

    public long MerchantId { get; set; }

    public long OrderId { get; set; }

    [Column(StringLength = 32), Description("订单号")]
    public string OrderNo { get; set; } = string.Empty;

    public long UserId { get; set; }

    [Description("该订单本活动产生的折扣合计")] public decimal DiscountAmount { get; set; }

    [Description("满赠发放的券张数")] public int GiftCouponCount { get; set; }
}

/// <summary>活动参与记录（商品行级）：每商品每活动一条，报表按商品维度统计。</summary>
[Table(Name = "marketing_activity_record_item")]
[Index("idx_activity_record_item_record", "RecordId")]
public sealed class MarketingActivityRecordItem : BaseEntity
{
    public long RecordId { get; set; }

    public long ActivityId { get; set; }

    [Column(StringLength = 32), Description("订单号，便于按订单检索")] public string OrderNo { get; set; } = string.Empty;
    public long SkuId { get; set; }

    [Column(StringLength = 128), Description("商品名称快照")]
    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    [Description("商品行小计（原价）")] public decimal ItemAmount { get; set; }

    [Description("商品行优惠金额")] public decimal DiscountAmount { get; set; }
}

/// <summary>券使用记录（订单级）：每券每订单一条，供券效果报表。</summary>
[Table(Name = "coupon_record")]
[Index("idx_coupon_record_activity", "CouponActivityId")]
[Index("idx_coupon_record_order", "OrderNo")]
public sealed class CouponRecord : BaseEntity
{
    public long CouponActivityId { get; set; }

    [Column(StringLength = 64), Description("券活动名称快照")]
    public string CouponActivityName { get; set; } = string.Empty;

    public long CouponTemplateId { get; set; }

    public long UserCouponId { get; set; }

    public long PlatformId { get; set; }

    public long MerchantId { get; set; }

    public long OrderId { get; set; }

    [Column(StringLength = 32), Description("订单号")]
    public string OrderNo { get; set; } = string.Empty;

    public long UserId { get; set; }

    [Description("本券在本订单的抵扣合计")] public decimal DiscountAmount { get; set; }
}

/// <summary>券使用记录（商品行级）。</summary>
[Table(Name = "coupon_record_item")]
[Index("idx_coupon_record_item_record", "RecordId")]
public sealed class CouponRecordItem : BaseEntity
{
    public long RecordId { get; set; }

    public long CouponActivityId { get; set; }

    [Column(StringLength = 32), Description("订单号")] public string OrderNo { get; set; } = string.Empty;
    public long SkuId { get; set; }

    [Column(StringLength = 128), Description("商品名称快照")]
    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal ItemAmount { get; set; }

    public decimal DiscountAmount { get; set; }
}

/// <summary>平台营销配置：券/活动计算优先级（每个平台一条，缺省券优先）。</summary>
[Table(Name = "marketing_config")]
[Index("uk_marketing_config_platform", "PlatformId", IsUnique = true)]
public sealed class MarketingConfig : BaseEntity
{
    public long PlatformId { get; set; }

    [Description("1活动优先 2券优先")] public int DiscountPriority { get; set; } = (int)Enums.DiscountPriority.CouponFirst;
}
