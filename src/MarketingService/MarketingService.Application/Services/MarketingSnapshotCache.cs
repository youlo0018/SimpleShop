using MarketingService.Domain.Entity;
using MarketingService.Domain.Enums;
using MarketingService.Domain.IRepository;
using Microsoft.Extensions.Caching.Memory;

namespace MarketingService.Application.Services;

/// <summary>
/// 平台营销快照缓存（配置 + 启用活动 + 活动范围）：营销配置读多写少，
/// 商品列表到手价、结算预览、活动专区每次都查同一批数据，按平台缓存可显著减少数据库往返。
///
/// 一致性策略：
/// 1) 显式失效：保存活动 / 活动启停 / 保存平台配置时立即移除缓存，运营改动与自动化测试即时生效；
/// 2) 短 TTL 兜底（30s）：直接改库（脚本/补偿）也能在 30 秒内自动恢复一致；
/// 3) 时间窗口不缓存：快照保存"全部启用活动"，使用时按当前时间过滤 StartAt/EndAt，
///    避免缓存导致活动提前生效或过期后仍可用。
/// </summary>
public sealed class MarketingSnapshotCache(IMemoryCache cache, IMarketingActivityRepository repository)
{
    /// <summary>缓存兜底有效期：即使漏掉显式失效，最多 30 秒后也会读到最新配置。</summary>
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(30);

    /// <summary>读取平台快照：命中直接返回；未命中则查配置/启用活动/范围后写入缓存。</summary>
    /// <param name="platformId">平台 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>平台营销快照（全局优先级 + 全部启用活动 + 活动范围）。</returns>
    public async Task<MarketingPlatformSnapshot> GetAsync(long platformId, CancellationToken cancellationToken)
    {
        var key = CacheKey(platformId);
        if (cache.TryGetValue(key, out MarketingPlatformSnapshot? cached) && cached is not null) return cached;

        var config = await repository.GetConfigAsync(platformId);
        var activities = await repository.ListEnabledActivitiesAsync(platformId);
        var targets = (await repository.ListTargetsAsync(activities.Select(item => item.Id).ToList()))
            .GroupBy(item => item.ActivityId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var snapshot = new MarketingPlatformSnapshot
        {
            Priority = config?.DiscountPriority ?? (int)DiscountPriority.CouponFirst,
            Activities = activities,
            ActivityTargets = targets
        };
        cache.Set(key, snapshot, Ttl);
        return snapshot;
    }

    /// <summary>失效平台快照：活动/配置写操作后调用，保证下一次计算读到最新数据。</summary>
    /// <param name="platformId">平台 ID；&lt;=0 时忽略（无平台上下文的写操作）。</param>
    public void Invalidate(long platformId)
    {
        if (platformId > 0) cache.Remove(CacheKey(platformId));
    }

    /// <summary>缓存键：平台维度，避免跨平台串数据。</summary>
    private static string CacheKey(long platformId) => $"marketing:platform-snapshot:{platformId}";
}

/// <summary>平台营销快照（不可变）：全局优先级 + 全部启用活动 + 活动范围；时间窗口由使用方过滤。</summary>
public sealed class MarketingPlatformSnapshot
{
    /// <summary>平台配置的全局优先级（1 活动优先 / 2 券优先），缺省券优先。</summary>
    public int Priority { get; init; } = (int)DiscountPriority.CouponFirst;

    /// <summary>平台下全部启用活动（未过滤时间窗口，使用时按当前时间过滤）。</summary>
    public List<MarketingActivity> Activities { get; init; } = [];

    /// <summary>活动 ID → 范围明细（范围匹配在内存完成）。</summary>
    public Dictionary<long, List<MarketingActivityTarget>> ActivityTargets { get; init; } = [];
}
