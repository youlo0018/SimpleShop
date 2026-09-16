using FluentValidation;

namespace MarketingService.Application.Features.UserMarketing;

public class ClaimableCouponsValidator : AbstractValidator<ClaimableCouponsQuery>
{
    public ClaimableCouponsValidator() => RuleFor(x => x.PlatformId).GreaterThan(0).WithMessage("请先选择平台");
}

public class ClaimCouponValidator : AbstractValidator<ClaimCouponCommand>
{
    public ClaimCouponValidator() => RuleFor(x => x.CouponActivityId).GreaterThan(0).WithMessage("优惠券不能为空");
}

public class MyCouponsValidator : AbstractValidator<MyCouponsQuery>
{
    public MyCouponsValidator()
        => RuleFor(x => x.Status).Must(value => value is null or 1 or 2 or 3).WithMessage("券状态不正确");
}

public class SettlePreviewValidator : AbstractValidator<SettlePreviewCommand>
{
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
