using CommunalService.Domain.Infrastructure;
using FreeSql;
using MarketingService.Domain.Entity;
using MarketingService.Domain.Enums;
using MarketingService.Domain.IRepository;

namespace MarketingService.Infrastructure.Repository;

/// <summary>活动仓储实现：分页组合查询与范围/记录/配置的读写，软删除由全局过滤器与基类保证。</summary>
public sealed class MarketingActivityRepository(IFreeSql freeSql)
    : BaseRepository<MarketingActivity>(freeSql), IMarketingActivityRepository
{
    /// <inheritdoc />
    public async Task<(List<MarketingActivity> Items, long Total)> QueryPagedAsync(
        string keyword, int? activityType, int? scopeType,
        long platformId, long merchantId, int page, int pageSize)
    {
        var query = freeSql.Select<MarketingActivity>()
            .WhereIf(platformId > 0, item => item.PlatformId == platformId)
            .WhereIf(merchantId > 0, item => item.MerchantId == merchantId)
            .WhereIf(activityType.HasValue, item => item.ActivityType == activityType!.Value)
            .WhereIf(scopeType.HasValue, item => item.ScopeType == scopeType!.Value)
            .WhereIf(!string.IsNullOrWhiteSpace(keyword), item => item.Name.Contains(keyword));
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(item => item.CreatedAt).Page(page, pageSize).ToListAsync();
        return (items, total);
    }

    /// <inheritdoc />
    public Task<List<MarketingActivity>> ListEnabledActivitiesAsync(long platformId)
        => freeSql.Select<MarketingActivity>()
            .Where(item => item.PlatformId == platformId && item.IsEnabled)
            // 计算引擎按创建顺序遍历：优惠力度相同时结果确定，满赠兜底取最早创建的活动。
            .OrderBy(item => item.CreatedAt).OrderBy(item => item.Id)
            .ToListAsync();

    /// <inheritdoc />
    public Task<List<MarketingActivityTarget>> ListTargetsAsync(IReadOnlyCollection<long> activityIds)
        => activityIds.Count == 0
            ? Task.FromResult(new List<MarketingActivityTarget>())
            : freeSql.Select<MarketingActivityTarget>().Where(item => activityIds.Contains(item.ActivityId)).ToListAsync();

    /// <inheritdoc />
    public async Task ReplaceTargetsAsync(long activityId, IEnumerable<MarketingActivityTarget> targets)
    {
        // 范围是"整组替换"语义：旧范围先软删，新范围批量插入（全局过滤器自动排除软删数据）。
        await freeSql.Update<MarketingActivityTarget>()
            .Set(item => item.IsDeleted, true)
            .Set(item => item.DeletedAt, DateTime.Now)
            .Where(item => item.ActivityId == activityId)
            .ExecuteAffrowsAsync();
        var list = targets.ToList();
        if (list.Count > 0) await freeSql.Insert(list).ExecuteAffrowsAsync();
    }

    /// <inheritdoc />
    public async Task InsertRecordAsync(MarketingActivityRecord record, IReadOnlyCollection<MarketingActivityRecordItem> items)
    {
        await freeSql.Insert(record).ExecuteAffrowsAsync();
        // 必须用 List：IReadOnlyCollection 会命中 FreeSql 的单实体重载，导致 ToSql NRE。
        if (items.Count > 0) await freeSql.Insert(items.ToList()).ExecuteAffrowsAsync();
    }

    /// <inheritdoc />
    public Task<List<MarketingActivityRecord>> ListRecordsByOrderNoAsync(string orderNo)
        => freeSql.Select<MarketingActivityRecord>().Where(item => item.OrderNo == orderNo).ToListAsync();

    /// <inheritdoc />
    public Task<bool> HasRecordAsync(string orderNo)
        => freeSql.Select<MarketingActivityRecord>().Where(item => item.OrderNo == orderNo).AnyAsync();

    /// <inheritdoc />
    public Task UpdateRecordGiftCountAsync(long recordId, int giftCouponCount)
        => freeSql.Update<MarketingActivityRecord>()
            .Set(item => item.GiftCouponCount, giftCouponCount)
            .Where(item => item.Id == recordId)
            .ExecuteAffrowsAsync();

    /// <inheritdoc />
    public Task<List<MarketingActivityRecord>> ListRecordsAsync(long activityId, DateTime? from, DateTime? to, int page, int pageSize)
        => freeSql.Select<MarketingActivityRecord>()
            .Where(item => item.ActivityId == activityId)
            .WhereIf(from.HasValue, item => item.CreatedAt >= from!.Value)
            .WhereIf(to.HasValue, item => item.CreatedAt <= to!.Value)
            .OrderByDescending(item => item.CreatedAt).Page(page, pageSize).ToListAsync();

    /// <inheritdoc />
    public async Task<(List<MarketingActivityRecord> Items, long Total)> QueryRecordsAsync(
        long platformId, long merchantId, long activityId, DateTime? from, DateTime? to, int page, int pageSize)
    {
        var query = freeSql.Select<MarketingActivityRecord>()
            .WhereIf(platformId > 0, item => item.PlatformId == platformId)
            .WhereIf(merchantId > 0, item => item.MerchantId == merchantId)
            .WhereIf(activityId > 0, item => item.ActivityId == activityId)
            .WhereIf(from.HasValue, item => item.CreatedAt >= from!.Value)
            .WhereIf(to.HasValue, item => item.CreatedAt <= to!.Value);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(item => item.CreatedAt).Page(page, pageSize).ToListAsync();
        return (items, total);
    }

    /// <inheritdoc />
    public async Task<List<MarketingActivitySummaryRow>> SummarizeActivitiesAsync(long platformId, long merchantId, DateTime? from, DateTime? to)
    {
        // 报表按月/活动维度聚合，MVP 数据量下在内存聚合，避免 FreeSql 分组聚合的方言差异。
        var records = await freeSql.Select<MarketingActivityRecord>()
            .WhereIf(platformId > 0, item => item.PlatformId == platformId)
            .WhereIf(merchantId > 0, item => item.MerchantId == merchantId)
            .WhereIf(from.HasValue, item => item.CreatedAt >= from!.Value)
            .WhereIf(to.HasValue, item => item.CreatedAt <= to!.Value)
            .ToListAsync();
        return records
            .GroupBy(item => new { item.ActivityId, item.ActivityName, item.ActivityType })
            .Select(group => new MarketingActivitySummaryRow(
                group.Key.ActivityId, group.Key.ActivityName, group.Key.ActivityType,
                group.LongCount(), group.Sum(item => item.DiscountAmount), group.Sum(item => item.GiftCouponCount)))
            .OrderByDescending(item => item.DiscountAmount)
            .ToList();
    }

    /// <inheritdoc />
    public Task<List<MarketingActivityRecordItem>> ListRecordItemsAsync(IReadOnlyCollection<long> recordIds, string? orderNo = null)
    {
        if (orderNo is not null)
            return freeSql.Select<MarketingActivityRecordItem>().Where(item => item.OrderNo == orderNo).ToListAsync();
        return recordIds.Count == 0
            ? Task.FromResult(new List<MarketingActivityRecordItem>())
            : freeSql.Select<MarketingActivityRecordItem>().Where(item => recordIds.Contains(item.RecordId)).ToListAsync();
    }

    /// <inheritdoc />
    public Task<MarketingConfig?> GetConfigAsync(long platformId)
        => freeSql.Select<MarketingConfig>().Where(item => item.PlatformId == platformId).FirstAsync();

    /// <inheritdoc />
    public async Task SaveConfigAsync(MarketingConfig config)
    {
        if (config.Id > 0) await freeSql.Update<MarketingConfig>().SetSource(config).ExecuteAffrowsAsync();
        else await freeSql.Insert(config).ExecuteAffrowsAsync();
    }
}

/// <summary>券仓储实现：模板/券活动/范围/用户券/使用记录。</summary>
public sealed class MarketingCouponRepository(IFreeSql freeSql)
    : BaseRepository<CouponTemplate>(freeSql), IMarketingCouponRepository
{
    /// <inheritdoc />
    public async Task<(List<CouponTemplate> Items, long Total)> QueryTemplatesPagedAsync(
        string keyword, int? couponType, long platformId, long merchantId, int page, int pageSize)
    {
        var query = freeSql.Select<CouponTemplate>()
            .WhereIf(platformId > 0, item => item.PlatformId == platformId)
            .WhereIf(merchantId > 0, item => item.MerchantId == merchantId)
            .WhereIf(couponType.HasValue, item => item.CouponType == couponType!.Value)
            .WhereIf(!string.IsNullOrWhiteSpace(keyword), item => item.Name.Contains(keyword));
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(item => item.CreatedAt).Page(page, pageSize).ToListAsync();
        return (items, total);
    }

    /// <inheritdoc />
    public async Task<(List<CouponActivity> Items, long Total)> QueryActivitiesPagedAsync(
        string keyword, long platformId, long merchantId, int page, int pageSize)
    {
        var query = freeSql.Select<CouponActivity>()
            .WhereIf(platformId > 0, item => item.PlatformId == platformId)
            .WhereIf(merchantId > 0, item => item.MerchantId == merchantId)
            .WhereIf(!string.IsNullOrWhiteSpace(keyword), item => item.Name.Contains(keyword));
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(item => item.CreatedAt).Page(page, pageSize).ToListAsync();
        return (items, total);
    }

    /// <inheritdoc />
    public Task<CouponActivity?> GetActivityByIdAsync(long id)
        => freeSql.Select<CouponActivity>().Where(item => item.Id == id).FirstAsync();

    /// <inheritdoc />
    public async Task<bool> InsertCouponActivityAsync(CouponActivity entity)
        => await freeSql.Insert(entity).ExecuteAffrowsAsync() > 0;

    /// <inheritdoc />
    public async Task<bool> UpdateCouponActivityAsync(CouponActivity entity)
        => await freeSql.Update<CouponActivity>().SetSource(entity).ExecuteAffrowsAsync() > 0;

    /// <inheritdoc />
    public async Task<bool> UpdateCouponActivityColumnsAsync(long id, object columns)
        => await freeSql.Update<CouponActivity>().Where(item => item.Id == id).SetDto(columns).ExecuteAffrowsAsync() > 0;

    /// <inheritdoc />
    public async Task<bool> InsertUserCouponAsync(UserCoupon entity)
        => await freeSql.Insert(entity).ExecuteAffrowsAsync() > 0;

    /// <inheritdoc />
    public Task<List<CouponActivity>> ListEnabledActivitiesAsync(long platformId, DateTime now)
        => freeSql.Select<CouponActivity>()
            .Where(item => item.PlatformId == platformId && item.IsEnabled && item.StartAt <= now && (item.EndAt == null || item.EndAt > now))
            // 显式排序：无 ORDER BY 时 PostgreSQL 返回堆顺序，表现为"随机排序"。
            .OrderByDescending(item => item.CreatedAt).OrderByDescending(item => item.Id)
            .ToListAsync();

    /// <inheritdoc />
    public Task<List<CouponActivity>> ListActivitiesByIdsAsync(IReadOnlyCollection<long> ids)
        => ids.Count == 0
            ? Task.FromResult(new List<CouponActivity>())
            : freeSql.Select<CouponActivity>().Where(item => ids.Contains(item.Id)).ToListAsync();

    /// <inheritdoc />
    public Task<List<CouponTemplate>> ListTemplatesByIdsAsync(IReadOnlyCollection<long> ids)
        => ids.Count == 0
            ? Task.FromResult(new List<CouponTemplate>())
            : freeSql.Select<CouponTemplate>().Where(item => ids.Contains(item.Id)).ToListAsync();

    /// <inheritdoc />
    public Task<List<CouponActivityTarget>> ListTargetsAsync(IReadOnlyCollection<long> couponActivityIds)
        => couponActivityIds.Count == 0
            ? Task.FromResult(new List<CouponActivityTarget>())
            : freeSql.Select<CouponActivityTarget>().Where(item => couponActivityIds.Contains(item.CouponActivityId)).ToListAsync();

    /// <inheritdoc />
    public async Task ReplaceTargetsAsync(long couponActivityId, IEnumerable<CouponActivityTarget> targets)
    {
        await freeSql.Update<CouponActivityTarget>()
            .Set(item => item.IsDeleted, true)
            .Set(item => item.DeletedAt, DateTime.Now)
            .Where(item => item.CouponActivityId == couponActivityId)
            .ExecuteAffrowsAsync();
        var list = targets.ToList();
        if (list.Count > 0) await freeSql.Insert(list).ExecuteAffrowsAsync();
    }

    /// <inheritdoc />
    public async Task<int> CountUserIssuedAsync(long couponActivityId, long userId)
        => (int)await freeSql.Select<UserCoupon>()
            .Where(item => item.CouponActivityId == couponActivityId && item.UserId == userId)
            .CountAsync();

    /// <inheritdoc />
    public Task<List<UserCoupon>> ListUserCouponsAsync(long userId, bool unusedOnly, long couponActivityId = 0)
    {
        var now = DateTime.Now;
        return freeSql.Select<UserCoupon>()
            .Where(item => item.UserId == userId)
            .WhereIf(unusedOnly, item => item.Status == (int)UserCouponStatus.Unused && item.ExpireAt > now)
            .WhereIf(couponActivityId > 0, item => item.CouponActivityId == couponActivityId)
            .OrderByDescending(item => item.ReceivedAt).ToListAsync();
    }

    /// <inheritdoc />
    public Task<List<UserCoupon>> ListUserCouponsByIdsAsync(long userId, IReadOnlyCollection<long> couponIds)
        => couponIds.Count == 0
            ? Task.FromResult(new List<UserCoupon>())
            : freeSql.Select<UserCoupon>().Where(item => item.UserId == userId && couponIds.Contains(item.Id)).ToListAsync();

    /// <inheritdoc />
    public async Task<bool> MarkUserCouponUsedAsync(long userCouponId, long orderId, string orderNo)
    {
        // 条件更新兜底并发：只有仍处于"未使用"的券才能被占用，避免同一张券被两单同时用掉。
        var affected = await freeSql.Update<UserCoupon>()
            .Set(item => item.Status, (int)UserCouponStatus.Used)
            .Set(item => item.UsedAt, DateTime.Now)
            .Set(item => item.UsedOrderNo, orderNo)
            .Where(item => item.Id == userCouponId && item.Status == (int)UserCouponStatus.Unused)
            .ExecuteAffrowsAsync();
        return affected > 0;
    }

    /// <inheritdoc />
    public async Task<bool> ReleaseUserCouponAsync(string orderNo)
    {
        var affected = await freeSql.Update<UserCoupon>()
            .Set(item => item.Status, (int)UserCouponStatus.Unused)
            .Set(item => item.UsedAt, (DateTime?)null)
            .Set(item => item.UsedOrderNo, (string?)null)
            .Where(item => item.UsedOrderNo == orderNo && item.Status == (int)UserCouponStatus.Used)
            .ExecuteAffrowsAsync();
        return affected > 0;
    }

    /// <inheritdoc />
    public async Task<bool> TryIncreaseIssuedAsync(long couponActivityId)
    {
        // 条件自增在 SQL 层完成，避免高并发下超发；PostgreSQL 保留 FreeSql 的 PascalCase 列名。
        var affected = await freeSql.Ado.ExecuteNonQueryAsync(
            "update \"coupon_activity\" set \"IssuedCount\" = \"IssuedCount\" + 1 where \"Id\" = @id and \"IssuedCount\" < \"TotalStock\" and \"IsDeleted\" = false",
            new { id = couponActivityId });
        return affected > 0;
    }

    /// <inheritdoc />
    public async Task InsertCouponRecordAsync(CouponRecord record, IReadOnlyCollection<CouponRecordItem> items)
    {
        await freeSql.Insert(record).ExecuteAffrowsAsync();
        // 必须用 List：IReadOnlyCollection 会命中 FreeSql 的单实体重载，导致 ToSql NRE。
        if (items.Count > 0) await freeSql.Insert(items.ToList()).ExecuteAffrowsAsync();
    }

    /// <inheritdoc />
    public Task<bool> HasCouponRecordAsync(string orderNo)
        => freeSql.Select<CouponRecord>().Where(item => item.OrderNo == orderNo).AnyAsync();

    /// <inheritdoc />
    public Task<List<CouponRecord>> ListCouponRecordsAsync(long couponActivityId, DateTime? from, DateTime? to, int page, int pageSize)
        => freeSql.Select<CouponRecord>()
            .Where(item => item.CouponActivityId == couponActivityId)
            .WhereIf(from.HasValue, item => item.CreatedAt >= from!.Value)
            .WhereIf(to.HasValue, item => item.CreatedAt <= to!.Value)
            .OrderByDescending(item => item.CreatedAt).Page(page, pageSize).ToListAsync();

    /// <inheritdoc />
    public async Task<(List<CouponRecord> Items, long Total)> QueryCouponRecordsAsync(
        long platformId, long merchantId, long couponActivityId, DateTime? from, DateTime? to, int page, int pageSize)
    {
        var query = freeSql.Select<CouponRecord>()
            .WhereIf(platformId > 0, item => item.PlatformId == platformId)
            .WhereIf(merchantId > 0, item => item.MerchantId == merchantId)
            .WhereIf(couponActivityId > 0, item => item.CouponActivityId == couponActivityId)
            .WhereIf(from.HasValue, item => item.CreatedAt >= from!.Value)
            .WhereIf(to.HasValue, item => item.CreatedAt <= to!.Value);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(item => item.CreatedAt).Page(page, pageSize).ToListAsync();
        return (items, total);
    }

    /// <inheritdoc />
    public async Task<List<CouponSummaryRow>> SummarizeCouponsAsync(long platformId, long merchantId, DateTime? from, DateTime? to)
    {
        var records = await freeSql.Select<CouponRecord>()
            .WhereIf(platformId > 0, item => item.PlatformId == platformId)
            .WhereIf(merchantId > 0, item => item.MerchantId == merchantId)
            .WhereIf(from.HasValue, item => item.CreatedAt >= from!.Value)
            .WhereIf(to.HasValue, item => item.CreatedAt <= to!.Value)
            .ToListAsync();
        return records
            .GroupBy(item => new { item.CouponActivityId, item.CouponActivityName })
            .Select(group => new CouponSummaryRow(
                group.Key.CouponActivityId, group.Key.CouponActivityName,
                group.LongCount(), group.Sum(item => item.DiscountAmount)))
            .OrderByDescending(item => item.DiscountAmount)
            .ToList();
    }

    /// <inheritdoc />
    public Task<List<CouponRecordItem>> ListCouponRecordItemsAsync(IReadOnlyCollection<long> recordIds, string? orderNo = null)
    {
        if (orderNo is not null)
            return freeSql.Select<CouponRecordItem>().Where(item => item.OrderNo == orderNo).ToListAsync();
        return recordIds.Count == 0
            ? Task.FromResult(new List<CouponRecordItem>())
            : freeSql.Select<CouponRecordItem>().Where(item => recordIds.Contains(item.RecordId)).ToListAsync();
    }
}
