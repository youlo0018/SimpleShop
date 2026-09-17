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
    /// <summary>活动分页：按当前租户（平台=本平台全部活动；商户=本商户活动）裁剪后查询，返回 items/total/分页信息。</summary>
    public async Task<ApiResponse> Handle(ListActivitiesQuery request, CancellationToken cancellationToken)
    {
        var platformId = tenant.IsPlatform ? tenant.PlatformId : tenant.HasWildcard ? 0 : 0;
        var merchantId = tenant.IsMerchant ? tenant.MerchantId : 0;
        var (items, total) = await repository.QueryPagedAsync(
            request.Keyword, request.ActivityType, request.ScopeType, platformId, merchantId, request.Page, request.PageSize);
        return ApiResults.Ok(new { items, total, page = request.Page, pageSize = request.PageSize });
    }
}

/// <summary>活动详情：校验租户归属后返回活动与范围明细，供后台编辑回显。</summary>
public class GetActivityHandler(IMarketingActivityRepository repository, TenantContext tenant)
    : IRequestHandler<GetActivityQuery, ApiResponse>
{
    /// <summary>活动详情：返回活动实体与范围明细；跨租户访问返回 403。</summary>
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

/// <summary>保存活动：租户回填、满赠券活动归属校验、范围整组替换；新增与编辑共用。</summary>
public class SaveActivityHandler(
    IMarketingActivityRepository repository,
    IMarketingCouponRepository couponRepository,
    TenantContext tenant)
    : IRequestHandler<SaveActivityCommand, ApiResponse>
{
    /// <summary>保存活动：租户回填 → 校验满赠券活动归属 → 落库 → 整组替换范围；Id=0 新增，否则编辑（越权返回 403）。</summary>
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

/// <summary>活动启停：校验租户归属后更新 IsEnabled。</summary>
public class SetActivityEnabledHandler(IMarketingActivityRepository repository, TenantContext tenant)
    : IRequestHandler<SetActivityEnabledCommand, ApiResponse>
{
    /// <summary>活动启停：校验租户归属后条件更新 IsEnabled（停用不影响历史订单记录）。</summary>
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
    /// <summary>券模板分页：按租户裁剪（平台=本平台模板，商户=本商户模板）。</summary>
    public async Task<ApiResponse> Handle(ListCouponTemplatesQuery request, CancellationToken cancellationToken)
    {
        var platformId = tenant.IsPlatform ? tenant.PlatformId : 0;
        var merchantId = tenant.IsMerchant ? tenant.MerchantId : 0;
        var (items, total) = await repository.QueryTemplatesPagedAsync(
            request.Keyword, request.CouponType, platformId, merchantId, request.Page, request.PageSize);
        return ApiResults.Ok(new { items, total, page = request.Page, pageSize = request.PageSize });
    }
}

/// <summary>保存券模板：租户回填、0元减强制门槛 0；新增与编辑共用。</summary>
public class SaveCouponTemplateHandler(IMarketingCouponRepository repository, TenantContext tenant)
    : IRequestHandler<SaveCouponTemplateCommand, ApiResponse>
{
    /// <summary>保存券模板：租户回填 → 0元减强制门槛为 0 → 落库；返回模板 ID。</summary>
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

/// <summary>券模板启停：校验归属后更新。</summary>
public class SetCouponTemplateEnabledHandler(IMarketingCouponRepository repository, TenantContext tenant)
    : IRequestHandler<SetCouponTemplateEnabledCommand, ApiResponse>
{
    /// <summary>券模板启停：校验归属后更新；停用只影响新领取/发放。</summary>
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
    /// <summary>券活动分页：按租户裁剪，附带模板名称与优惠信息（templateType/templateThreshold/templateDiscountValue）便于后台直接展示。</summary>
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

/// <summary>券活动详情：返回券活动、范围与模板，供后台编辑回显。</summary>
public class GetCouponActivityHandler(IMarketingCouponRepository repository, TenantContext tenant)
    : IRequestHandler<GetCouponActivityQuery, ApiResponse>
{
    /// <summary>券活动详情：返回券活动、范围明细与模板；跨租户访问返回 403。</summary>
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

/// <summary>保存券活动：模板归属校验、租户回填、范围整组替换；新增与编辑共用。</summary>
public class SaveCouponActivityHandler(IMarketingCouponRepository repository, TenantContext tenant)
    : IRequestHandler<SaveCouponActivityCommand, ApiResponse>
{
    /// <summary>保存券活动：校验模板归属 → 租户回填 → 落库 → 整组替换范围；返回券活动 ID。</summary>
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

/// <summary>券活动启停：校验归属后更新。</summary>
public class SetCouponActivityEnabledHandler(IMarketingCouponRepository repository, TenantContext tenant)
    : IRequestHandler<SetCouponActivityEnabledCommand, ApiResponse>
{
    /// <summary>券活动启停：校验归属后更新；停用后不再发券，已发用户券仍可使用。</summary>
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
    /// <summary>读取平台营销配置：不存在时返回默认值（券优先），前端据此渲染单选项。</summary>
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

/// <summary>保存平台营销配置：仅平台账号，存在则更新否则新增。</summary>
public class SaveMarketingConfigHandler(IMarketingActivityRepository repository, TenantContext tenant)
    : IRequestHandler<SaveMarketingConfigCommand, ApiResponse>
{
    /// <summary>保存平台营销配置：不存在则新增、存在则更新优先级；仅平台账号可操作。</summary>
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
