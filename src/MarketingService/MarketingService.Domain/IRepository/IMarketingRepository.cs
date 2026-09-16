using CommunalService.Domain.Interfaces;
using MarketingService.Domain.Entity;

namespace MarketingService.Domain.IRepository;

/// <summary>活动效果聚合行（报表用）。</summary>
public sealed record MarketingActivitySummaryRow(
    long ActivityId, string ActivityName, int ActivityType, long OrderCount, decimal DiscountAmount, long GiftCouponCount);

/// <summary>券效果聚合行（报表用）。</summary>
public sealed record CouponSummaryRow(
    long CouponActivityId, string CouponActivityName, long CouponCount, decimal DiscountAmount);

/// <summary>
/// 活动仓储：活动/范围/参与记录/平台配置。参与记录与配置不单独暴露仓储接口，避免同库事务语义被拆散。
/// </summary>
public interface IMarketingActivityRepository : IBaseRepository<MarketingActivity>
{
    /// <summary>后台分页：平台/商户数据隔离在 Handler 完成，仓储只按过滤条件取数。</summary>
    Task<(List<MarketingActivity> Items, long Total)> QueryPagedAsync(
        string keyword, int? activityType, int? scopeType,
        long platformId, long merchantId, int page, int pageSize);

    /// <summary>计算引擎用：取平台下当前有效的启用活动（含平台活动与指定商户活动，范围匹配在内存完成）。</summary>
    Task<List<MarketingActivity>> ListEnabledAsync(long platformId, DateTime now);

    Task<List<MarketingActivityTarget>> ListTargetsAsync(IReadOnlyCollection<long> activityIds);

    /// <summary>整组替换活动范围（先软删旧范围再批量插入新范围）。</summary>
    Task ReplaceTargetsAsync(long activityId, IEnumerable<MarketingActivityTarget> targets);

    Task InsertRecordAsync(MarketingActivityRecord record, IReadOnlyCollection<MarketingActivityRecordItem> items);

    Task<List<MarketingActivityRecord>> ListRecordsByOrderNoAsync(string orderNo);

    Task<bool> HasRecordAsync(string orderNo);

    Task UpdateRecordGiftCountAsync(long recordId, int giftCouponCount);

    Task<List<MarketingActivityRecord>> ListRecordsAsync(long activityId, DateTime? from, DateTime? to, int page, int pageSize);

    /// <summary>报表：按租户/活动/时间分页取参与记录。</summary>
    Task<(List<MarketingActivityRecord> Items, long Total)> QueryRecordsAsync(
        long platformId, long merchantId, long activityId, DateTime? from, DateTime? to, int page, int pageSize);

    /// <summary>报表：按活动聚合（参与订单数、折扣总额、赠券数）。</summary>
    Task<List<MarketingActivitySummaryRow>> SummarizeActivitiesAsync(long platformId, long merchantId, DateTime? from, DateTime? to);

    Task<List<MarketingActivityRecordItem>> ListRecordItemsAsync(IReadOnlyCollection<long> recordIds, string? orderNo = null);

    Task<MarketingConfig?> GetConfigAsync(long platformId);

    Task SaveConfigAsync(MarketingConfig config);
}

/// <summary>
/// 券仓储：券模板/券活动/范围/用户券/使用记录。
/// </summary>
public interface IMarketingCouponRepository : IBaseRepository<CouponTemplate>
{
    Task<(List<CouponTemplate> Items, long Total)> QueryTemplatesPagedAsync(
        string keyword, int? couponType, long platformId, long merchantId, int page, int pageSize);

    Task<(List<CouponActivity> Items, long Total)> QueryActivitiesPagedAsync(
        string keyword, long platformId, long merchantId, int page, int pageSize);

    /// <summary>按 ID 取券活动（券活动的软删除由全局过滤器处理）。</summary>
    Task<CouponActivity?> GetActivityByIdAsync(long id);

    Task<bool> InsertCouponActivityAsync(CouponActivity entity);

    Task<bool> UpdateCouponActivityAsync(CouponActivity entity);

    /// <summary>券活动的字段级更新（UpdateColumns 的泛型基类版本会写到券模板表，必须走这个专用方法）。</summary>
    Task<bool> UpdateCouponActivityColumnsAsync(long id, object columns);

    Task<bool> InsertUserCouponAsync(UserCoupon entity);

    /// <summary>领券中心与计算引擎用：平台下当前有效的启用券活动。</summary>
    Task<List<CouponActivity>> ListEnabledActivitiesAsync(long platformId, DateTime now);

    Task<List<CouponActivity>> ListActivitiesByIdsAsync(IReadOnlyCollection<long> ids);

    Task<List<CouponTemplate>> ListTemplatesByIdsAsync(IReadOnlyCollection<long> ids);

    Task<List<CouponActivityTarget>> ListTargetsAsync(IReadOnlyCollection<long> couponActivityIds);

    Task ReplaceTargetsAsync(long couponActivityId, IEnumerable<CouponActivityTarget> targets);

    /// <summary>统计某券活动某用户的已发放张数（含所有来源），用于限领和库存校验。</summary>
    Task<int> CountUserIssuedAsync(long couponActivityId, long userId);

    /// <summary>用户券包（含模板/活动名称等展示信息由 Handler 组装）。unusedOnly=true 时只取未使用且未过期。</summary>
    Task<List<UserCoupon>> ListUserCouponsAsync(long userId, bool unusedOnly, long couponActivityId = 0);

    Task<List<UserCoupon>> ListUserCouponsByIdsAsync(long userId, IReadOnlyCollection<long> couponIds);

    /// <summary>原子占用：仅当仍为未使用时置为已使用，返回是否成功（防并发重复用券）。</summary>
    Task<bool> MarkUserCouponUsedAsync(long userCouponId, long orderId, string orderNo);

    /// <summary>订单取消时回退占用：仅当被该订单占用且未支付时恢复为未使用。</summary>
    Task<bool> ReleaseUserCouponAsync(string orderNo);

    /// <summary>券活动发行量 +1（带库存条件，防超发），返回是否成功。</summary>
    Task<bool> TryIncreaseIssuedAsync(long couponActivityId);

    Task InsertCouponRecordAsync(CouponRecord record, IReadOnlyCollection<CouponRecordItem> items);

    Task<bool> HasCouponRecordAsync(string orderNo);

    Task<List<CouponRecord>> ListCouponRecordsAsync(long couponActivityId, DateTime? from, DateTime? to, int page, int pageSize);

    /// <summary>报表：按租户/券活动/时间分页取用券记录。</summary>
    Task<(List<CouponRecord> Items, long Total)> QueryCouponRecordsAsync(
        long platformId, long merchantId, long couponActivityId, DateTime? from, DateTime? to, int page, int pageSize);

    /// <summary>报表：按券活动聚合（核销笔数、抵扣总额）。</summary>
    Task<List<CouponSummaryRow>> SummarizeCouponsAsync(long platformId, long merchantId, DateTime? from, DateTime? to);

    Task<List<CouponRecordItem>> ListCouponRecordItemsAsync(IReadOnlyCollection<long> recordIds, string? orderNo = null);
}
