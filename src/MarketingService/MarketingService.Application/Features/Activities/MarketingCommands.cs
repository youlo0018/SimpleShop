using CommunalService.Domain;
using MediatR;

namespace MarketingService.Application.Features.Activities;

/// <summary>活动/券活动范围输入项：ScopeType=Merchant 用 TargetMerchantIds；ScopeType=Products 用 TargetProducts。</summary>
public sealed record MarketingTargetInput
{
    /// <summary>商品所属商户 ID（指定商品范围时必填，用于引擎快速过滤）。</summary>
    public long MerchantId { get; init; }

    /// <summary>商品 SKU ID（指定商品范围的匹配键）。</summary>
    public long SkuId { get; init; }
}

/// <summary>保存活动（Id=0 新增，Id&gt;0 编辑）：平台账号可建平台活动或本平台商户活动，商户账号只能建自己的活动。</summary>
public sealed record SaveActivityCommand : IRequest<ApiResponse>
{
    /// <summary>活动 ID；0 表示新增。</summary>
    public long Id { get; init; }

    /// <summary>所属平台 ID；平台账号由网关租户强制回填，通配管理员可显式指定。</summary>
    public long PlatformId { get; init; }

    /// <summary>所属商户 ID；0=平台活动，大于 0=商户活动（商户账号由租户强制回填）。</summary>
    public long MerchantId { get; init; }

    /// <summary>活动名称（必填，≤64 字符）。</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>活动说明（可选，≤255 字符）。</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>活动类型：1 满减 / 2 满折 / 3 满赠。</summary>
    public int ActivityType { get; init; }

    /// <summary>门槛金额（元，≥0）：按商品行小计判断。</summary>
    public decimal Threshold { get; init; }

    /// <summary>优惠值：满减=金额（&gt;0）；满折=折扣率（0.01-0.99）；满赠忽略。</summary>
    public decimal DiscountValue { get; init; }

    /// <summary>满赠发放的券活动 ID（ActivityType=3 必填且必须属于当前租户）。</summary>
    public long GiftCouponActivityId { get; init; }

    /// <summary>参与范围：1 全部 / 2 指定商户 / 3 指定商户商品（商户活动不支持 2）。</summary>
    public int ScopeType { get; init; } = 1;

    /// <summary>指定商户 ID 列表（ScopeType=2 时必填）。</summary>
    public List<long> TargetMerchantIds { get; init; } = [];

    /// <summary>指定商品列表（ScopeType=3 时必填）。</summary>
    public List<MarketingTargetInput> TargetProducts { get; init; } = [];

    /// <summary>活动开始时间（含）。</summary>
    public DateTime StartAt { get; init; } = DateTime.Now;

    /// <summary>活动结束时间（不含）；null 表示长期。</summary>
    public DateTime? EndAt { get; init; }

    /// <summary>是否启用。</summary>
    public bool IsEnabled { get; init; } = true;
}

/// <summary>活动启停：置 IsEnabled（停用后不再参与新的优惠计算，历史记录保留）。</summary>
public sealed record SetActivityEnabledCommand(long Id, bool IsEnabled) : IRequest<ApiResponse>;

/// <summary>后台活动分页查询（按当前租户裁剪）。</summary>
public sealed record ListActivitiesQuery(
    string Keyword = "", int? ActivityType = null, int? ScopeType = null,
    int Page = 1, int PageSize = 10) : IRequest<ApiResponse>;

/// <summary>活动详情（含范围明细，后台编辑回显用）。</summary>
public sealed record GetActivityQuery(long Id) : IRequest<ApiResponse>;

/// <summary>保存券模板（Id=0 新增）：仅定义规则与有效天数，不直接发券。</summary>
public sealed record SaveCouponTemplateCommand : IRequest<ApiResponse>
{
    /// <summary>模板 ID；0 表示新增。</summary>
    public long Id { get; init; }

    /// <summary>所属平台 ID（平台账号由租户回填）。</summary>
    public long PlatformId { get; init; }

    /// <summary>所属商户 ID；0=平台模板（商户账号由租户回填）。</summary>
    public long MerchantId { get; init; }

    /// <summary>模板名称（必填，≤64 字符）。</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>使用说明（可选，≤255 字符）。</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>券类型：1 满减 / 2 满折 / 3 0元减。</summary>
    public int CouponType { get; init; }

    /// <summary>门槛金额（元）；0元减必须为 0。</summary>
    public decimal Threshold { get; init; }

    /// <summary>优惠值：满减/0元减=金额（&gt;0）；满折=折扣率（0.01-0.99）。</summary>
    public decimal DiscountValue { get; init; }

    /// <summary>领取后有效天数（1-365）。</summary>
    public int ValidDays { get; init; } = 7;

    /// <summary>是否启用。</summary>
    public bool IsEnabled { get; init; } = true;
}

/// <summary>券模板启停。</summary>
public sealed record SetCouponTemplateEnabledCommand(long Id, bool IsEnabled) : IRequest<ApiResponse>;

/// <summary>后台券模板分页查询。</summary>
public sealed record ListCouponTemplatesQuery(string Keyword = "", int? CouponType = null, int Page = 1, int PageSize = 10)
    : IRequest<ApiResponse>;

/// <summary>保存券活动（Id=0 新增）：券的实际发放与使用范围载体。</summary>
public sealed record SaveCouponActivityCommand : IRequest<ApiResponse>
{
    /// <summary>券活动 ID；0 表示新增。</summary>
    public long Id { get; init; }

    /// <summary>所属平台 ID（平台账号由租户回填）。</summary>
    public long PlatformId { get; init; }

    /// <summary>所属商户 ID；0=平台券活动（商户账号由租户回填）。</summary>
    public long MerchantId { get; init; }

    /// <summary>券活动名称（必填，≤64 字符，作为券展示名）。</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>使用的券模板 ID（必填，必须属于当前租户）。</summary>
    public long CouponTemplateId { get; init; }

    /// <summary>使用范围：1 全部 / 2 指定商户 / 3 指定商户商品（商户券活动不支持 2）。</summary>
    public int ScopeType { get; init; } = 1;

    /// <summary>指定商户 ID 列表（ScopeType=2 时必填）。</summary>
    public List<long> TargetMerchantIds { get; init; } = [];

    /// <summary>指定商品列表（ScopeType=3 时必填）。</summary>
    public List<MarketingTargetInput> TargetProducts { get; init; } = [];

    /// <summary>发行总量（1-1000000，领取与满赠共用）。</summary>
    public int TotalStock { get; init; } = 1000;

    /// <summary>每人限领数量（1-100，仅约束领券中心领取）。</summary>
    public int PerUserLimit { get; init; } = 1;

    /// <summary>是否可在领券中心领取；满赠专用券活动设为 false。</summary>
    public bool IsClaimable { get; init; } = true;

    /// <summary>发放开始时间（含）。</summary>
    public DateTime StartAt { get; init; } = DateTime.Now;

    /// <summary>发放结束时间（不含）；null 表示长期。</summary>
    public DateTime? EndAt { get; init; }

    /// <summary>是否启用。</summary>
    public bool IsEnabled { get; init; } = true;
}

/// <summary>券活动启停。</summary>
public sealed record SetCouponActivityEnabledCommand(long Id, bool IsEnabled) : IRequest<ApiResponse>;

/// <summary>后台券活动分页查询。</summary>
public sealed record ListCouponActivitiesQuery(string Keyword = "", int Page = 1, int PageSize = 10) : IRequest<ApiResponse>;

/// <summary>券活动详情（含范围与模板，后台编辑回显用）。</summary>
public sealed record GetCouponActivityQuery(long Id) : IRequest<ApiResponse>;

/// <summary>查询平台营销配置（缺省返回券优先）。</summary>
public sealed record GetMarketingConfigQuery(long PlatformId = 0) : IRequest<ApiResponse>;

/// <summary>保存平台营销配置：券/活动计算优先级（每平台一条）。</summary>
public sealed record SaveMarketingConfigCommand(long PlatformId = 0, int DiscountPriority = 2) : IRequest<ApiResponse>;
