using FluentValidation;

namespace MarketingService.Application.Features.UserMarketing;

/// <summary>领券中心校验：平台必选。</summary>
public class ClaimableCouponsValidator : AbstractValidator<ClaimableCouponsQuery>
{
    /// <summary>规则覆盖：平台必选（PlatformId&gt;0）。</summary>
    public ClaimableCouponsValidator() => RuleFor(x => x.PlatformId).GreaterThan(0).WithMessage("请先选择平台");
}

/// <summary>领券校验：券活动必选。</summary>
public class ClaimCouponValidator : AbstractValidator<ClaimCouponCommand>
{
    /// <summary>规则覆盖：券活动必选（CouponActivityId&gt;0）。</summary>
    public ClaimCouponValidator() => RuleFor(x => x.CouponActivityId).GreaterThan(0).WithMessage("优惠券不能为空");
}

/// <summary>券包查询校验：状态过滤只能是 null/1/2/3。</summary>
public class MyCouponsValidator : AbstractValidator<MyCouponsQuery>
{
    /// <summary>规则覆盖：状态过滤只能是 null/1/2/3。</summary>
    public MyCouponsValidator()
        => RuleFor(x => x.Status).Must(value => value is null or 1 or 2 or 3).WithMessage("券状态不正确");
}

/// <summary>结算预览校验：平台必选、商品行非空且价格/数量合法（数量 1-99 与下单一致）。</summary>
/// <summary>进行中活动校验：平台必选。</summary>
public class ActiveActivitiesValidator : AbstractValidator<ActiveActivitiesQuery>
{
    /// <summary>规则覆盖：PlatformId &gt; 0。</summary>
    public ActiveActivitiesValidator() => RuleFor(x => x.PlatformId).GreaterThan(0).WithMessage("请先选择平台");
}

/// <summary>结算预览校验：平台必选、商品行非空且价格/数量合法（数量 1-99 与下单一致）。</summary>
public class SettlePreviewValidator : AbstractValidator<SettlePreviewCommand>
{
    /// <summary>规则覆盖：平台必选、商品行非空、每行 SKU/价格/数量合法（数量 1-99 与下单一致）。</summary>
    public SettlePreviewValidator()
    {
        RuleFor(x => x.PlatformId).GreaterThan(0).WithMessage("请先选择平台");
        RuleFor(x => x.Items).NotEmpty().WithMessage("结算商品不能为空");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.SkuId).GreaterThan(0).WithMessage("SKU 不能为空");
            item.RuleFor(i => i.Price).GreaterThan(0).WithMessage("商品价格必须大于0");
            item.RuleFor(i => i.Quantity).InclusiveBetween(1, 99).WithMessage("数量必须为1-99");
        });
    }
}
