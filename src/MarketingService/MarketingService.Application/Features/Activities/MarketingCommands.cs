using CommunalService.Domain;
using MediatR;

namespace MarketingService.Application.Features.Activities;

/// <summary>活动范围输入：ScopeType=Merchant 用 TargetMerchantIds；ScopeType=Products 用 TargetProducts。</summary>
public sealed record MarketingTargetInput
{
    public long MerchantId { get; init; }
    public long SkuId { get; init; }
}

public sealed record SaveActivityCommand : IRequest<ApiResponse>
{
    public long Id { get; init; }
    public long PlatformId { get; init; }
    public long MerchantId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;

    /// <summary>1满减 2满折 3满赠。</summary>
    public int ActivityType { get; init; }

    public decimal Threshold { get; init; }
    public decimal DiscountValue { get; init; }
    public long GiftCouponActivityId { get; init; }

    /// <summary>1全部范围 2指定商户 3指定商户商品。</summary>
    public int ScopeType { get; init; } = 1;

    public List<long> TargetMerchantIds { get; init; } = [];
    public List<MarketingTargetInput> TargetProducts { get; init; } = [];
    public DateTime StartAt { get; init; } = DateTime.Now;
    public DateTime? EndAt { get; init; }
    public bool IsEnabled { get; init; } = true;
}

public sealed record SetActivityEnabledCommand(long Id, bool IsEnabled) : IRequest<ApiResponse>;

public sealed record ListActivitiesQuery(
    string Keyword = "", int? ActivityType = null, int? ScopeType = null,
    int Page = 1, int PageSize = 10) : IRequest<ApiResponse>;

public sealed record GetActivityQuery(long Id) : IRequest<ApiResponse>;

public sealed record SaveCouponTemplateCommand : IRequest<ApiResponse>
{
    public long Id { get; init; }
    public long PlatformId { get; init; }
    public long MerchantId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;

    /// <summary>1满减 2满折 3零元减。</summary>
    public int CouponType { get; init; }

    public decimal Threshold { get; init; }
    public decimal DiscountValue { get; init; }
    public int ValidDays { get; init; } = 7;
    public bool IsEnabled { get; init; } = true;
}

public sealed record SetCouponTemplateEnabledCommand(long Id, bool IsEnabled) : IRequest<ApiResponse>;

public sealed record ListCouponTemplatesQuery(string Keyword = "", int? CouponType = null, int Page = 1, int PageSize = 10)
    : IRequest<ApiResponse>;

public sealed record SaveCouponActivityCommand : IRequest<ApiResponse>
{
    public long Id { get; init; }
    public long PlatformId { get; init; }
    public long MerchantId { get; init; }
    public string Name { get; init; } = string.Empty;
    public long CouponTemplateId { get; init; }
    public int ScopeType { get; init; } = 1;
    public List<long> TargetMerchantIds { get; init; } = [];
    public List<MarketingTargetInput> TargetProducts { get; init; } = [];
    public int TotalStock { get; init; } = 1000;
    public int PerUserLimit { get; init; } = 1;
    public bool IsClaimable { get; init; } = true;
    public DateTime StartAt { get; init; } = DateTime.Now;
    public DateTime? EndAt { get; init; }
    public bool IsEnabled { get; init; } = true;
}

public sealed record SetCouponActivityEnabledCommand(long Id, bool IsEnabled) : IRequest<ApiResponse>;

public sealed record ListCouponActivitiesQuery(string Keyword = "", int Page = 1, int PageSize = 10) : IRequest<ApiResponse>;

public sealed record GetCouponActivityQuery(long Id) : IRequest<ApiResponse>;

public sealed record GetMarketingConfigQuery(long PlatformId = 0) : IRequest<ApiResponse>;

public sealed record SaveMarketingConfigCommand(long PlatformId = 0, int DiscountPriority = 2) : IRequest<ApiResponse>;
