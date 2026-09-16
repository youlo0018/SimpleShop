using FluentValidation;
using MarketingService.Domain.Enums;

namespace MarketingService.Application.Features.Activities;

/// <summary>活动参数校验：按类型约束门槛/优惠值，满赠必须指定券活动，范围类型与目标必须匹配。</summary>
public class SaveActivityValidator : AbstractValidator<SaveActivityCommand>
{
    public SaveActivityValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("活动名称不能为空").MaximumLength(64).WithMessage("活动名称不能超过64个字符");
        RuleFor(x => x.Description).MaximumLength(255).WithMessage("活动说明不能超过255个字符");
        RuleFor(x => x.ActivityType).Must(value => Enum.IsDefined(typeof(ActivityType), value)).WithMessage("活动类型不正确");
        RuleFor(x => x.Threshold).GreaterThanOrEqualTo(0).WithMessage("门槛金额不能为负数");
        RuleFor(x => x.ScopeType)
            .Must(value => value is (int)MarketingScopeType.All or (int)MarketingScopeType.Merchant or (int)MarketingScopeType.Products)
            .WithMessage("参与范围不正确");
        RuleFor(x => x.TargetMerchantIds)
            .NotEmpty().When(x => x.ScopeType == (int)MarketingScopeType.Merchant)
            .WithMessage("指定商户活动必须选择商户");
        RuleFor(x => x.TargetProducts)
            .NotEmpty().When(x => x.ScopeType == (int)MarketingScopeType.Products)
            .WithMessage("指定商品活动必须选择商品");
        RuleForEach(x => x.TargetProducts).ChildRules(target =>
        {
            target.RuleFor(item => item.SkuId).GreaterThan(0).WithMessage("商品不能为空");
        });
        RuleFor(x => x.DiscountValue)
            .GreaterThan(0m).When(x => x.ActivityType == (int)ActivityType.FullReduce)
            .WithMessage("满减金额必须大于0");
        RuleFor(x => x.DiscountValue)
            .InclusiveBetween(0.01m, 0.99m).When(x => x.ActivityType == (int)ActivityType.FullDiscount)
            .WithMessage("折扣率必须在0.01-0.99之间（0.85=8.5折）");
        RuleFor(x => x.GiftCouponActivityId)
            .GreaterThan(0).When(x => x.ActivityType == (int)ActivityType.GiftCoupon)
            .WithMessage("满赠活动必须选择赠品券活动");
        RuleFor(x => x.EndAt)
            .Must((command, endAt) => !endAt.HasValue || endAt.Value > command.StartAt)
            .WithMessage("结束时间必须晚于开始时间");
    }
}

public class SetActivityEnabledValidator : AbstractValidator<SetActivityEnabledCommand>
{
    public SetActivityEnabledValidator() => RuleFor(x => x.Id).GreaterThan(0).WithMessage("活动不能为空");
}

public class ListActivitiesValidator : AbstractValidator<ListActivitiesQuery>
{
    public ListActivitiesValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("页码不能小于1");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("每页数量必须为1-100");
        RuleFor(x => x.Keyword).MaximumLength(64).WithMessage("关键词不能超过64个字符");
    }
}

public class GetActivityValidator : AbstractValidator<GetActivityQuery>
{
    public GetActivityValidator() => RuleFor(x => x.Id).GreaterThan(0).WithMessage("活动不能为空");
}

/// <summary>券模板校验：0元减门槛固定 0，满折折扣率 0.01-0.99，满减/0元减优惠额必须大于 0。</summary>
public class SaveCouponTemplateValidator : AbstractValidator<SaveCouponTemplateCommand>
{
    public SaveCouponTemplateValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("券模板名称不能为空").MaximumLength(64).WithMessage("名称不能超过64个字符");
        RuleFor(x => x.Description).MaximumLength(255).WithMessage("使用说明不能超过255个字符");
        RuleFor(x => x.CouponType).Must(value => Enum.IsDefined(typeof(CouponType), value)).WithMessage("券类型不正确");
        RuleFor(x => x.Threshold).GreaterThanOrEqualTo(0).WithMessage("门槛金额不能为负数");
        RuleFor(x => x.Threshold).Equal(0m).When(x => x.CouponType == (int)CouponType.NoThresholdReduce)
            .WithMessage("0元减券门槛必须为0");
        RuleFor(x => x.DiscountValue)
            .GreaterThan(0m).When(x => x.CouponType is (int)CouponType.FullReduce or (int)CouponType.NoThresholdReduce)
            .WithMessage("优惠金额必须大于0");
        RuleFor(x => x.DiscountValue)
            .InclusiveBetween(0.01m, 0.99m).When(x => x.CouponType == (int)CouponType.FullDiscount)
            .WithMessage("折扣率必须在0.01-0.99之间（0.85=8.5折）");
        RuleFor(x => x.ValidDays).InclusiveBetween(1, 365).WithMessage("有效天数必须为1-365");
    }
}

public class SetCouponTemplateEnabledValidator : AbstractValidator<SetCouponTemplateEnabledCommand>
{
    public SetCouponTemplateEnabledValidator() => RuleFor(x => x.Id).GreaterThan(0).WithMessage("券模板不能为空");
}

public class ListCouponTemplatesValidator : AbstractValidator<ListCouponTemplatesQuery>
{
    public ListCouponTemplatesValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("页码不能小于1");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("每页数量必须为1-100");
        RuleFor(x => x.Keyword).MaximumLength(64).WithMessage("关键词不能超过64个字符");
    }
}

/// <summary>券活动校验：发行量/限领/范围目标必须合法。</summary>
public class SaveCouponActivityValidator : AbstractValidator<SaveCouponActivityCommand>
{
    public SaveCouponActivityValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("券活动名称不能为空").MaximumLength(64).WithMessage("名称不能超过64个字符");
        RuleFor(x => x.CouponTemplateId).GreaterThan(0).WithMessage("请选择券模板");
        RuleFor(x => x.ScopeType)
            .Must(value => value is (int)MarketingScopeType.All or (int)MarketingScopeType.Merchant or (int)MarketingScopeType.Products)
            .WithMessage("参与范围不正确");
        RuleFor(x => x.TargetMerchantIds)
            .NotEmpty().When(x => x.ScopeType == (int)MarketingScopeType.Merchant)
            .WithMessage("指定商户券活动必须选择商户");
        RuleFor(x => x.TargetProducts)
            .NotEmpty().When(x => x.ScopeType == (int)MarketingScopeType.Products)
            .WithMessage("指定商品券活动必须选择商品");
        RuleForEach(x => x.TargetProducts).ChildRules(target =>
        {
            target.RuleFor(item => item.SkuId).GreaterThan(0).WithMessage("商品不能为空");
        });
        RuleFor(x => x.TotalStock).InclusiveBetween(1, 1000000).WithMessage("发行量必须为1-1000000");
        RuleFor(x => x.PerUserLimit).InclusiveBetween(1, 100).WithMessage("每人限领必须为1-100");
        RuleFor(x => x.EndAt)
            .Must((command, endAt) => !endAt.HasValue || endAt.Value > command.StartAt)
            .WithMessage("结束时间必须晚于开始时间");
    }
}

public class SetCouponActivityEnabledValidator : AbstractValidator<SetCouponActivityEnabledCommand>
{
    public SetCouponActivityEnabledValidator() => RuleFor(x => x.Id).GreaterThan(0).WithMessage("券活动不能为空");
}

public class ListCouponActivitiesValidator : AbstractValidator<ListCouponActivitiesQuery>
{
    public ListCouponActivitiesValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("页码不能小于1");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("每页数量必须为1-100");
        RuleFor(x => x.Keyword).MaximumLength(64).WithMessage("关键词不能超过64个字符");
    }
}

public class GetCouponActivityValidator : AbstractValidator<GetCouponActivityQuery>
{
    public GetCouponActivityValidator() => RuleFor(x => x.Id).GreaterThan(0).WithMessage("券活动不能为空");
}

public class SaveMarketingConfigValidator : AbstractValidator<SaveMarketingConfigCommand>
{
    public SaveMarketingConfigValidator()
        => RuleFor(x => x.DiscountPriority).Must(value => Enum.IsDefined(typeof(DiscountPriority), value))
            .WithMessage("优惠计算优先级不正确");
}
