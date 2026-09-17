using FluentValidation;
using MarketingService.Domain.Enums;

namespace MarketingService.Application.Features.Activities;

/// <summary>活动参数校验：按类型约束门槛/优惠值，满赠必须指定券活动，范围类型与目标必须匹配。</summary>
public class SaveActivityValidator : AbstractValidator<SaveActivityCommand>
{
    /// <summary>规则覆盖：名称/说明长度、类型枚举、门槛非负、范围与目标匹配、满减金额/满折折扣率/满赠券活动、起止时间顺序。</summary>
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

/// <summary>活动启停校验：活动 ID 必填。</summary>
public class SetActivityEnabledValidator : AbstractValidator<SetActivityEnabledCommand>
{
    /// <summary>规则覆盖：活动 ID &gt; 0。</summary>
    public SetActivityEnabledValidator() => RuleFor(x => x.Id).GreaterThan(0).WithMessage("活动不能为空");
}

/// <summary>活动分页校验：页码、页大小与关键词长度。</summary>
public class ListActivitiesValidator : AbstractValidator<ListActivitiesQuery>
{
    /// <summary>规则覆盖：页码≥1、每页 1-100、关键词≤64。</summary>
    public ListActivitiesValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("页码不能小于1");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("每页数量必须为1-100");
        RuleFor(x => x.Keyword).MaximumLength(64).WithMessage("关键词不能超过64个字符");
    }
}

/// <summary>活动详情校验：活动 ID 必填。</summary>
public class GetActivityValidator : AbstractValidator<GetActivityQuery>
{
    /// <summary>规则覆盖：活动 ID &gt; 0。</summary>
    public GetActivityValidator() => RuleFor(x => x.Id).GreaterThan(0).WithMessage("活动不能为空");
}

/// <summary>券模板校验：0元减门槛固定 0，满折折扣率 0.01-0.99，满减/0元减优惠额必须大于 0。</summary>
public class SaveCouponTemplateValidator : AbstractValidator<SaveCouponTemplateCommand>
{
    /// <summary>规则覆盖：名称/说明长度、类型枚举、0元减门槛必须为 0、满减/0元减金额&gt;0、满折折扣率 0.01-0.99、有效天数 1-365。</summary>
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

/// <summary>券模板启停校验：券模板 ID 必填。</summary>
public class SetCouponTemplateEnabledValidator : AbstractValidator<SetCouponTemplateEnabledCommand>
{
    /// <summary>规则覆盖：券模板 ID &gt; 0。</summary>
    public SetCouponTemplateEnabledValidator() => RuleFor(x => x.Id).GreaterThan(0).WithMessage("券模板不能为空");
}

/// <summary>券模板分页校验：页码、页大小与关键词长度。</summary>
public class ListCouponTemplatesValidator : AbstractValidator<ListCouponTemplatesQuery>
{
    /// <summary>规则覆盖：页码≥1、每页 1-100、关键词≤64。</summary>
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
    /// <summary>规则覆盖：名称、模板必选、范围与目标匹配、发行量 1-1000000、每人限领 1-100、起止时间顺序。</summary>
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

/// <summary>券活动启停校验：券活动 ID 必填。</summary>
public class SetCouponActivityEnabledValidator : AbstractValidator<SetCouponActivityEnabledCommand>
{
    /// <summary>规则覆盖：券活动 ID &gt; 0。</summary>
    public SetCouponActivityEnabledValidator() => RuleFor(x => x.Id).GreaterThan(0).WithMessage("券活动不能为空");
}

/// <summary>券活动分页校验：页码、页大小与关键词长度。</summary>
public class ListCouponActivitiesValidator : AbstractValidator<ListCouponActivitiesQuery>
{
    /// <summary>规则覆盖：页码≥1、每页 1-100、关键词≤64。</summary>
    public ListCouponActivitiesValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("页码不能小于1");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("每页数量必须为1-100");
        RuleFor(x => x.Keyword).MaximumLength(64).WithMessage("关键词不能超过64个字符");
    }
}

/// <summary>券活动详情校验：券活动 ID 必填。</summary>
public class GetCouponActivityValidator : AbstractValidator<GetCouponActivityQuery>
{
    /// <summary>规则覆盖：券活动 ID &gt; 0。</summary>
    public GetCouponActivityValidator() => RuleFor(x => x.Id).GreaterThan(0).WithMessage("券活动不能为空");
}

/// <summary>营销配置校验：优先级必须为 DiscountPriority 已定义值。</summary>
public class SaveMarketingConfigValidator : AbstractValidator<SaveMarketingConfigCommand>
{
    /// <summary>规则覆盖：优先级必须是 DiscountPriority 已定义值（1 活动优先 / 2 券优先）。</summary>
    public SaveMarketingConfigValidator()
        => RuleFor(x => x.DiscountPriority).Must(value => Enum.IsDefined(typeof(DiscountPriority), value))
            .WithMessage("优惠计算优先级不正确");
}
