using CommunalService.Domain;
using CommunalService.Domain.Enums;
using MediatR;
using MarketingService.Domain.Entity;
using MarketingService.Domain.Enums;
using MarketingService.Domain.IRepository;

namespace MarketingService.Application.Features.Activities;

/// <summary>
/// 平台活动管理：平台账号维护本平台的全部活动（平台活动 + 本平台商户活动），商户账号只能维护自己的活动。
/// 赠券必须挂到同平台（商户活动还要求同商户）的券活动上，避免跨租户发券。
/// </summary>
public class ListActivitiesHandler(IMarketingActivityRepository repository, TenantContext tenant)
    : IRequestHandler<ListActivitiesQuery, ApiResponse>
{
    public async Task<ApiResponse> Handle(ListActivitiesQuery request, CancellationToken cancellationToken)
    {
        var platformId = tenant.IsPlatform ? tenant.PlatformId : tenant.HasWildcard ? 0 : 0;
        var merchantId = tenant.IsMerchant ? tenant.MerchantId : 0;
        var (items, total) = await repository.QueryPagedAsync(
            request.Keyword, request.ActivityType, request.ScopeType, platformId, merchantId, request.Page, request.PageSize);
        return ApiResults.Ok(new { items, total, page = request.Page, pageSize = request.PageSize });
    }
}

public class GetActivityHandler(IMarketingActivityRepository repository, TenantContext tenant)
    : IRequestHandler<GetActivityQuery, ApiResponse>
{
    public async Task<ApiResponse> Handle(GetActivityQuery request, CancellationToken cancellationToken)
    {
        var activity = await repository.GetByIdAsync(request.Id);
        if (activity is null) return ApiResults.Fail(BaseApiResponseCode.NotFound, "活动不存在");
        if (!InScope(activity, tenant)) return ApiResults.Fail(BaseApiResponseCode.Forbidden, "无权查看该活动");
        var targets = await repository.ListTargetsAsync([activity.Id]);
        return ApiResults.Ok(new { activity, targets });
    }

    private static bool InScope(MarketingActivity activity, TenantContext tenant)
    {
        if (tenant.HasWildcard) return true;
        if (tenant.IsPlatform) return activity.PlatformId == tenant.PlatformId;
        if (tenant.IsMerchant) return activity.PlatformId == tenant.PlatformId && activity.MerchantId == tenant.MerchantId;
        return false;
    }
}

public class SaveActivityHandler(
    IMarketingActivityRepository repository,
    IMarketingCouponRepository couponRepository,
    TenantContext tenant)
    : IRequestHandler<SaveActivityCommand, ApiResponse>
{
    public async Task<ApiResponse> Handle(SaveActivityCommand request, CancellationToken cancellationToken)
    {
        if (!tenant.HasWildcard && !tenant.IsPlatform && !tenant.IsMerchant)
            return ApiResults.Fail(BaseApiResponseCode.Forbidden, "无权维护活动");

        // 租户回填：商户只能建本商户活动且范围不得指定其他商户；平台建本平台活动。
        var platformId = tenant.IsPlatform ? tenant.PlatformId : request.PlatformId;
        var merchantId = tenant.IsMerchant ? tenant.MerchantId : request.MerchantId;
        if (platformId <= 0) return ApiResults.Fail(BaseApiResponseCode.BadRequest, "缺少平台");
        if (tenant.IsMerchant && request.ScopeType == (int)MarketingScopeType.Merchant)
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "商户活动不支持指定商户范围");

        if (request.ActivityType == (int)ActivityType.GiftCoupon)
        {
            var giftActivity = await couponRepository.GetActivityByIdAsync(request.GiftCouponActivityId);
            if (giftActivity is null || giftActivity.PlatformId != platformId ||
                (tenant.IsMerchant && giftActivity.MerchantId != merchantId))
                return ApiResults.Fail(BaseApiResponseCode.BadRequest, "赠品券活动不存在或不属于当前租户");
        }

        MarketingActivity entity;
        if (request.Id > 0)
        {
            var existing = await repository.GetByIdAsync(request.Id);
            if (existing is null) return ApiResults.Fail(BaseApiResponseCode.NotFound, "活动不存在");
            if (!tenant.HasWildcard && existing.PlatformId != tenant.PlatformId)
                return ApiResults.Fail(BaseApiResponseCode.Forbidden, "无权修改该活动");
            if (tenant.IsMerchant && existing.MerchantId != tenant.MerchantId)
                return ApiResults.Fail(BaseApiResponseCode.Forbidden, "无权修改该活动");
            entity = existing;
        }
        else
        {
            entity = new MarketingActivity { PlatformId = platformId, MerchantId = merchantId };
        }

        entity.PlatformId = platformId;
        if (request.Id == 0) entity.MerchantId = merchantId;
        entity.Name = request.Name.Trim();
        entity.Description = request.Description?.Trim() ?? string.Empty;
        entity.ActivityType = request.ActivityType;
        entity.Threshold = request.Threshold;
        entity.DiscountValue = request.DiscountValue;
        entity.GiftCouponActivityId = request.ActivityType == (int)ActivityType.GiftCoupon ? request.GiftCouponActivityId : 0;
        entity.ScopeType = request.ScopeType;
        entity.StartAt = request.StartAt;
        entity.EndAt = request.EndAt;
        entity.IsEnabled = request.IsEnabled;
        entity.UpdatedAt = DateTime.Now;

        if (entity.Id == 0) await repository.InsertAsync(entity);
        else await repository.UpdateAsync(entity);
        await repository.ReplaceTargetsAsync(entity.Id, BuildTargets(entity, request, tenant));
        return ApiResults.Ok(new { success = true, id = entity.Id });
    }

    internal static List<MarketingActivityTarget> BuildTargets(MarketingActivity entity, SaveActivityCommand request, TenantContext tenant)
    {
        var targets = new List<MarketingActivityTarget>();
        if (request.ScopeType == (int)MarketingScopeType.Merchant)
        {
            targets.AddRange(request.TargetMerchantIds.Distinct().Select(merchantId => new MarketingActivityTarget
            {
                ActivityId = entity.Id,
                TargetType = (int)MarketingTargetType.Merchant,
                TargetId = merchantId
            }));
        }
        else if (request.ScopeType == (int)MarketingScopeType.Products)
        {
            targets.AddRange(request.TargetProducts
                .Where(item => item.SkuId > 0)
                .GroupBy(item => item.SkuId)
                .Select(group => new MarketingActivityTarget
                {
                    ActivityId = entity.Id,
                    TargetType = (int)MarketingTargetType.Product,
                    TargetId = group.Key,
                    MerchantId = tenant.IsMerchant ? tenant.MerchantId : group.First().MerchantId
                }));
        }
        return targets;
    }
}

public class SetActivityEnabledHandler(IMarketingActivityRepository repository, TenantContext tenant)
    : IRequestHandler<SetActivityEnabledCommand, ApiResponse>
{
    public async Task<ApiResponse> Handle(SetActivityEnabledCommand request, CancellationToken cancellationToken)
    {
        var activity = await repository.GetByIdAsync(request.Id);
        if (activity is null) return ApiResults.Fail(BaseApiResponseCode.NotFound, "活动不存在");
        if (!tenant.HasWildcard && activity.PlatformId != tenant.PlatformId)
            return ApiResults.Fail(BaseApiResponseCode.Forbidden, "无权操作该活动");
        if (tenant.IsMerchant && activity.MerchantId != tenant.MerchantId)
            return ApiResults.Fail(BaseApiResponseCode.Forbidden, "无权操作该活动");
        await repository.UpdateColumnsAsync(activity.Id, new { IsEnabled = request.IsEnabled, UpdatedAt = DateTime.Now });
        return ApiResults.Ok(new { success = true });
    }
}

/// <summary>券模板管理：模板只描述优惠规则，租户回填与活动一致。</summary>
public class ListCouponTemplatesHandler(IMarketingCouponRepository repository, TenantContext tenant)
    : IRequestHandler<ListCouponTemplatesQuery, ApiResponse>
{
    public async Task<ApiResponse> Handle(ListCouponTemplatesQuery request, CancellationToken cancellationToken)
    {
        var platformId = tenant.IsPlatform ? tenant.PlatformId : 0;
        var merchantId = tenant.IsMerchant ? tenant.MerchantId : 0;
        var (items, total) = await repository.QueryTemplatesPagedAsync(
            request.Keyword, request.CouponType, platformId, merchantId, request.Page, request.PageSize);
        return ApiResults.Ok(new { items, total, page = request.Page, pageSize = request.PageSize });
    }
}

public class SaveCouponTemplateHandler(IMarketingCouponRepository repository, TenantContext tenant)
    : IRequestHandler<SaveCouponTemplateCommand, ApiResponse>
{
    public async Task<ApiResponse> Handle(SaveCouponTemplateCommand request, CancellationToken cancellationToken)
    {
        if (!tenant.HasWildcard && !tenant.IsPlatform && !tenant.IsMerchant)
            return ApiResults.Fail(BaseApiResponseCode.Forbidden, "无权维护券模板");

        var platformId = tenant.IsPlatform ? tenant.PlatformId : request.PlatformId;
        var merchantId = tenant.IsMerchant ? tenant.MerchantId : request.MerchantId;
        if (platformId <= 0) return ApiResults.Fail(BaseApiResponseCode.BadRequest, "缺少平台");

        CouponTemplate entity;
        if (request.Id > 0)
        {
            entity = await repository.GetByIdAsync(request.Id);
            if (entity is null) return ApiResults.Fail(BaseApiResponseCode.NotFound, "券模板不存在");
            if (!tenant.HasWildcard && entity.PlatformId != tenant.PlatformId)
                return ApiResults.Fail(BaseApiResponseCode.Forbidden, "无权修改该券模板");
            if (tenant.IsMerchant && entity.MerchantId != tenant.MerchantId)
                return ApiResults.Fail(BaseApiResponseCode.Forbidden, "无权修改该券模板");
        }
        else
        {
            entity = new CouponTemplate { PlatformId = platformId, MerchantId = merchantId };
        }

        entity.Name = request.Name.Trim();
        entity.Description = request.Description?.Trim() ?? string.Empty;
        entity.CouponType = request.CouponType;
        entity.Threshold = request.CouponType == (int)CouponType.NoThresholdReduce ? 0m : request.Threshold;
        entity.DiscountValue = request.DiscountValue;
        entity.ValidDays = request.ValidDays;
        entity.IsEnabled = request.IsEnabled;
        entity.UpdatedAt = DateTime.Now;

        if (entity.Id == 0) await repository.InsertAsync(entity);
        else await repository.UpdateAsync(entity);
        return ApiResults.Ok(new { success = true, id = entity.Id });
    }
}

public class SetCouponTemplateEnabledHandler(IMarketingCouponRepository repository, TenantContext tenant)
    : IRequestHandler<SetCouponTemplateEnabledCommand, ApiResponse>
{
    public async Task<ApiResponse> Handle(SetCouponTemplateEnabledCommand request, CancellationToken cancellationToken)
    {
        var template = await repository.GetByIdAsync(request.Id);
        if (template is null) return ApiResults.Fail(BaseApiResponseCode.NotFound, "券模板不存在");
        if (!tenant.HasWildcard && template.PlatformId != tenant.PlatformId)
            return ApiResults.Fail(BaseApiResponseCode.Forbidden, "无权操作该券模板");
        if (tenant.IsMerchant && template.MerchantId != tenant.MerchantId)
            return ApiResults.Fail(BaseApiResponseCode.Forbidden, "无权操作该券模板");
        await repository.UpdateColumnsAsync(template.Id, new { IsEnabled = request.IsEnabled, UpdatedAt = DateTime.Now });
        return ApiResults.Ok(new { success = true });
    }
}

/// <summary>券活动管理：实际发券与使用范围的载体，券模板与范围必须属于当前租户。</summary>
public class ListCouponActivitiesHandler(IMarketingCouponRepository repository, TenantContext tenant)
    : IRequestHandler<ListCouponActivitiesQuery, ApiResponse>
{
    public async Task<ApiResponse> Handle(ListCouponActivitiesQuery request, CancellationToken cancellationToken)
    {
        var platformId = tenant.IsPlatform ? tenant.PlatformId : 0;
        var merchantId = tenant.IsMerchant ? tenant.MerchantId : 0;
        var (items, total) = await repository.QueryActivitiesPagedAsync(
            request.Keyword, platformId, merchantId, request.Page, request.PageSize);
        // 列表带上模板名称，便于后台直接展示优惠内容。
        var templates = (await repository.ListTemplatesByIdsAsync(items.Select(item => item.CouponTemplateId).Distinct().ToList()))
            .ToDictionary(item => item.Id);
        var shaped = items.Select(item => new
        {
            item.Id,
            item.PlatformId,
            item.MerchantId,
            item.Name,
            item.CouponTemplateId,
            TemplateName = templates.GetValueOrDefault(item.CouponTemplateId)?.Name ?? string.Empty,
            TemplateType = templates.GetValueOrDefault(item.CouponTemplateId)?.CouponType ?? 0,
            TemplateThreshold = templates.GetValueOrDefault(item.CouponTemplateId)?.Threshold ?? 0m,
            TemplateDiscountValue = templates.GetValueOrDefault(item.CouponTemplateId)?.DiscountValue ?? 0m,
            item.ScopeType,
            item.TotalStock,
            item.IssuedCount,
            item.PerUserLimit,
            item.IsClaimable,
            item.StartAt,
            item.EndAt,
            item.IsEnabled,
            item.CreatedAt
        }).ToList();
        return ApiResults.Ok(new { items = shaped, total, page = request.Page, pageSize = request.PageSize });
    }
}

public class GetCouponActivityHandler(IMarketingCouponRepository repository, TenantContext tenant)
    : IRequestHandler<GetCouponActivityQuery, ApiResponse>
{
    public async Task<ApiResponse> Handle(GetCouponActivityQuery request, CancellationToken cancellationToken)
    {
        var activity = await repository.GetActivityByIdAsync(request.Id);
        if (activity is null) return ApiResults.Fail(BaseApiResponseCode.NotFound, "券活动不存在");
        if (!tenant.HasWildcard && activity.PlatformId != tenant.PlatformId)
            return ApiResults.Fail(BaseApiResponseCode.Forbidden, "无权查看该券活动");
        if (tenant.IsMerchant && activity.MerchantId != tenant.MerchantId)
            return ApiResults.Fail(BaseApiResponseCode.Forbidden, "无权查看该券活动");
        var targets = await repository.ListTargetsAsync([activity.Id]);
        var template = (await repository.ListTemplatesByIdsAsync([activity.CouponTemplateId])).FirstOrDefault();
        return ApiResults.Ok(new { activity, targets, template });
    }
}

public class SaveCouponActivityHandler(IMarketingCouponRepository repository, TenantContext tenant)
    : IRequestHandler<SaveCouponActivityCommand, ApiResponse>
{
    public async Task<ApiResponse> Handle(SaveCouponActivityCommand request, CancellationToken cancellationToken)
    {
        if (!tenant.HasWildcard && !tenant.IsPlatform && !tenant.IsMerchant)
            return ApiResults.Fail(BaseApiResponseCode.Forbidden, "无权维护券活动");

        var platformId = tenant.IsPlatform ? tenant.PlatformId : request.PlatformId;
        var merchantId = tenant.IsMerchant ? tenant.MerchantId : request.MerchantId;
        if (platformId <= 0) return ApiResults.Fail(BaseApiResponseCode.BadRequest, "缺少平台");
        if (tenant.IsMerchant && request.ScopeType == (int)MarketingScopeType.Merchant)
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "商户券活动不支持指定商户范围");

        var template = await repository.GetByIdAsync(request.CouponTemplateId);
        if (template is null || template.PlatformId != platformId ||
            (tenant.IsMerchant && template.MerchantId != merchantId))
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "券模板不存在或不属于当前租户");

        CouponActivity entity;
        if (request.Id > 0)
        {
            entity = await repository.GetActivityByIdAsync(request.Id);
            if (entity is null) return ApiResults.Fail(BaseApiResponseCode.NotFound, "券活动不存在");
            if (!tenant.HasWildcard && entity.PlatformId != tenant.PlatformId)
                return ApiResults.Fail(BaseApiResponseCode.Forbidden, "无权修改该券活动");
            if (tenant.IsMerchant && entity.MerchantId != tenant.MerchantId)
                return ApiResults.Fail(BaseApiResponseCode.Forbidden, "无权修改该券活动");
        }
        else
        {
            entity = new CouponActivity { PlatformId = platformId, MerchantId = merchantId };
        }

        entity.Name = request.Name.Trim();
        entity.CouponTemplateId = request.CouponTemplateId;
        entity.ScopeType = request.ScopeType;
        entity.TotalStock = request.TotalStock;
        entity.PerUserLimit = request.PerUserLimit;
        entity.IsClaimable = request.IsClaimable;
        entity.StartAt = request.StartAt;
        entity.EndAt = request.EndAt;
        entity.IsEnabled = request.IsEnabled;
        entity.UpdatedAt = DateTime.Now;

        if (entity.Id == 0) await repository.InsertCouponActivityAsync(entity);
        else await repository.UpdateCouponActivityAsync(entity);
        await repository.ReplaceTargetsAsync(entity.Id, BuildTargets(entity, request, tenant));
        return ApiResults.Ok(new { success = true, id = entity.Id });
    }

    internal static List<CouponActivityTarget> BuildTargets(CouponActivity entity, SaveCouponActivityCommand request, TenantContext tenant)
    {
        var targets = new List<CouponActivityTarget>();
        if (request.ScopeType == (int)MarketingScopeType.Merchant)
        {
            targets.AddRange(request.TargetMerchantIds.Distinct().Select(merchantId => new CouponActivityTarget
            {
                CouponActivityId = entity.Id,
                TargetType = (int)MarketingTargetType.Merchant,
                TargetId = merchantId
            }));
        }
        else if (request.ScopeType == (int)MarketingScopeType.Products)
        {
            targets.AddRange(request.TargetProducts
                .Where(item => item.SkuId > 0)
                .GroupBy(item => item.SkuId)
                .Select(group => new CouponActivityTarget
                {
                    CouponActivityId = entity.Id,
                    TargetType = (int)MarketingTargetType.Product,
                    TargetId = group.Key,
                    MerchantId = tenant.IsMerchant ? tenant.MerchantId : group.First().MerchantId
                }));
        }
        return targets;
    }
}

public class SetCouponActivityEnabledHandler(IMarketingCouponRepository repository, TenantContext tenant)
    : IRequestHandler<SetCouponActivityEnabledCommand, ApiResponse>
{
    public async Task<ApiResponse> Handle(SetCouponActivityEnabledCommand request, CancellationToken cancellationToken)
    {
        var activity = await repository.GetActivityByIdAsync(request.Id);
        if (activity is null) return ApiResults.Fail(BaseApiResponseCode.NotFound, "券活动不存在");
        if (!tenant.HasWildcard && activity.PlatformId != tenant.PlatformId)
            return ApiResults.Fail(BaseApiResponseCode.Forbidden, "无权操作该券活动");
        if (tenant.IsMerchant && activity.MerchantId != tenant.MerchantId)
            return ApiResults.Fail(BaseApiResponseCode.Forbidden, "无权操作该券活动");
        await repository.UpdateCouponActivityColumnsAsync(activity.Id, new { IsEnabled = request.IsEnabled, UpdatedAt = DateTime.Now });
        return ApiResults.Ok(new { success = true });
    }
}

/// <summary>营销配置：每平台一条，控制先算券还是先算活动的全局优先级。</summary>
public class GetMarketingConfigHandler(IMarketingActivityRepository repository, TenantContext tenant)
    : IRequestHandler<GetMarketingConfigQuery, ApiResponse>
{
    public async Task<ApiResponse> Handle(GetMarketingConfigQuery request, CancellationToken cancellationToken)
    {
        var platformId = tenant.IsPlatform ? tenant.PlatformId : request.PlatformId;
        var config = platformId > 0 ? await repository.GetConfigAsync(platformId) : null;
        return ApiResults.Ok(new
        {
            platformId,
            discountPriority = config?.DiscountPriority ?? (int)DiscountPriority.CouponFirst
        });
    }
}

public class SaveMarketingConfigHandler(IMarketingActivityRepository repository, TenantContext tenant)
    : IRequestHandler<SaveMarketingConfigCommand, ApiResponse>
{
    public async Task<ApiResponse> Handle(SaveMarketingConfigCommand request, CancellationToken cancellationToken)
    {
        var platformId = tenant.IsPlatform ? tenant.PlatformId : request.PlatformId;
        if (platformId <= 0) return ApiResults.Fail(BaseApiResponseCode.BadRequest, "请选择平台");
        var config = await repository.GetConfigAsync(platformId);
        if (config is null)
        {
            config = new MarketingConfig { PlatformId = platformId, DiscountPriority = request.DiscountPriority };
            await repository.SaveConfigAsync(config);
        }
        else
        {
            config.DiscountPriority = request.DiscountPriority;
            config.UpdatedAt = DateTime.Now;
            await repository.SaveConfigAsync(config);
        }
        return ApiResults.Ok(new { success = true });
    }
}
