using CommunalService.Domain;
using CommunalService.Domain.Contracts.Messages;
using MediatR;
using MarketingService.Domain.Enums;

namespace MarketingService.Application.Features.UserMarketing;

/// <summary>结算商品输入（购物车/提交页）：价格与数量由前端提交，服务端只用它做优惠计算。</summary>
public sealed record MarketingSettleItemInput
{
    /// <summary>商品 SKU ID（范围匹配与结果回传的键）。</summary>
    public long SkuId { get; init; }

    /// <summary>商品所属平台 ID（与活动/券平台一致才可用）。</summary>
    public long PlatformId { get; init; }

    /// <summary>商品所属商户 ID（商户活动/商户券匹配用）。</summary>
    public long MerchantId { get; init; }

    /// <summary>商品名称快照（参与记录展示）。</summary>
    public string ProductName { get; init; } = string.Empty;

    /// <summary>商品单价（元，必须大于 0）。</summary>
    public decimal Price { get; init; }

    /// <summary>数量（1-99，与下单校验一致）。</summary>
    public int Quantity { get; init; }
}

/// <summary>领券中心查询：指定要展示哪个平台的券活动（小程序按当前选中平台传）。</summary>
public sealed record ClaimableCouponsQuery(long PlatformId) : IRequest<ApiResponse>;

/// <summary>领取优惠券：指定券活动；限领与库存由 Handler 校验并条件自增防超发。</summary>
public sealed record ClaimCouponCommand(long CouponActivityId) : IRequest<ApiResponse>;

/// <summary>我的券包查询：Status 为展示状态过滤（1 未使用 / 2 已使用 / 3 已过期），null 表示全部。</summary>
public sealed record MyCouponsQuery(int? Status = null) : IRequest<ApiResponse>;

/// <summary>进行中活动/券活动（首页优惠专区、活动卡展示用，无需登录）。</summary>
public sealed record ActiveActivitiesQuery(long PlatformId, long MerchantId = 0) : IRequest<ApiResponse>;

/// <summary>购物车/提交页结算预览：不落任何数据，仅返回可参与活动/可用券与浮动金额。</summary>
public sealed record SettlePreviewCommand : IRequest<ApiResponse>
{
    /// <summary>下单平台 ID（活动与券按平台过滤）。</summary>
    public long PlatformId { get; init; }

    /// <summary>结算商品行（至少一行）。</summary>
    public List<MarketingSettleItemInput> Items { get; init; } = [];

    /// <summary>用户勾选使用的用户券 ID；null=自动使用全部可用券，[]=本单不使用券。</summary>
    public List<long>? SelectedUserCouponIds { get; init; }

    /// <summary>转换为内部结算请求（补齐登录用户；不携带订单信息，纯预览）。</summary>
    /// <param name="userId">当前登录用户 ID，用于加载其可用券。</param>
    /// <returns>优惠引擎可直接消费的结算请求。</returns>
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
