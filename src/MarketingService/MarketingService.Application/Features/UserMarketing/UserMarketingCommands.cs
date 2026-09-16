using CommunalService.Domain;
using CommunalService.Domain.Contracts.Messages;
using MediatR;
using MarketingService.Domain.Enums;

namespace MarketingService.Application.Features.UserMarketing;

/// <summary>结算商品输入（购物车/提交页）。</summary>
public sealed record MarketingSettleItemInput
{
    public long SkuId { get; init; }
    public long PlatformId { get; init; }
    public long MerchantId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int Quantity { get; init; }
}

public sealed record ClaimableCouponsQuery(long PlatformId) : IRequest<ApiResponse>;

public sealed record ClaimCouponCommand(long CouponActivityId) : IRequest<ApiResponse>;

public sealed record MyCouponsQuery(int? Status = null) : IRequest<ApiResponse>;

/// <summary>购物车/提交页结算预览：不落任何数据，仅返回可参与活动/可用券与浮动金额。</summary>
public sealed record SettlePreviewCommand : IRequest<ApiResponse>
{
    public long PlatformId { get; init; }
    public List<MarketingSettleItemInput> Items { get; init; } = [];

    /// <summary>null=自动使用全部可用券；[]=本单不使用券。</summary>
    public List<long>? SelectedUserCouponIds { get; init; }

    public MarketingSettleRequest ToRequest(long userId) => new()
    {
        PlatformId = PlatformId,
        UserId = userId,
        SelectedUserCouponIds = SelectedUserCouponIds,
        Items = Items.Select(item => new MarketingSettleItem
        {
            SkuId = item.SkuId,
            PlatformId = item.PlatformId,
            MerchantId = item.MerchantId,
            ProductName = item.ProductName,
            Price = item.Price,
            Quantity = item.Quantity
        }).ToList()
    };
}
