using CommunalService.Domain.Interfaces;
using MarketingService.Domain.Entity;

namespace MarketingService.Domain.IRepository;

/// <summary>活动效果聚合行（报表用，在内存中按活动聚合而成）。</summary>
/// <param name="ActivityId">活动 ID。</param>
/// <param name="ActivityName">活动名称快照（取记录中的名称）。</param>
/// <param name="ActivityType">活动类型快照：1 满减 / 2 满折 / 3 满赠。</param>
/// <param name="OrderCount">参与订单数（记录条数）。</param>
/// <param name="DiscountAmount">折扣总额（元）。</param>
/// <param name="GiftCouponCount">满赠发放券张数合计。</param>
public sealed record MarketingActivitySummaryRow(
    long ActivityId, string ActivityName, int ActivityType, long OrderCount, decimal DiscountAmount, long GiftCouponCount);

/// <summary>券效果聚合行（报表用，在内存中按券活动聚合而成）。</summary>
/// <param name="CouponActivityId">券活动 ID。</param>
/// <param name="CouponActivityName">券活动名称快照。</param>
/// <param name="CouponCount">核销笔数（记录条数）。</param>
/// <param name="DiscountAmount">抵扣总额（元）。</param>
public sealed record CouponSummaryRow(
    long CouponActivityId, string CouponActivityName, long CouponCount, decimal DiscountAmount);

/// <summary>
/// 活动仓储：活动/范围/参与记录/平台配置。参与记录与配置不单独暴露仓储接口，避免同库事务语义被拆散。
/// 所有查询自动受 FreeSql 全局软删除过滤器约束。
/// </summary>
public interface IMarketingActivityRepository : IBaseRepository<MarketingActivity>
{
    /// <summary>后台活动分页：平台/商户数据隔离在 Handler 完成，仓储只按过滤条件取数。</summary>
    /// <param name="keyword">活动名称模糊匹配，空串不过滤。</param>
    /// <param name="activityType">活动类型过滤；null 不过滤。</param>
    /// <param name="scopeType">范围类型过滤；null 不过滤。</param>
    /// <param name="platformId">平台过滤；&lt;=0 不过滤（通配管理员）。</param>
    /// <param name="merchantId">商户过滤；&lt;=0 不过滤。</param>
    /// <param name="page">页码，从 1 开始。</param>
    /// <param name="pageSize">每页条数。</param>
    /// <returns>指定页数据与满足条件的总数（total 与取数同条件）。</returns>
    Task<(List<MarketingActivity> Items, long Total)> QueryPagedAsync(
        string keyword, int? activityType, int? scopeType,
        long platformId, long merchantId, int page, int pageSize);

    /// <summary>
    /// 计算引擎与活动专区用：取平台下全部"启用"活动（含未开始/已结束，时间窗口由使用方按当前时间过滤）。
    /// 不带时间过滤是为了让结果可被平台快照缓存复用；固定按 CreatedAt、Id 升序返回，保证优惠并列与满赠兜底结果确定。
    /// </summary>
    /// <param name="platformId">平台 ID。</param>
    /// <returns>平台下所有 IsEnabled 的活动（未过滤 StartAt/EndAt）。</returns>
    Task<List<MarketingActivity>> ListEnabledActivitiesAsync(long platformId);

    /// <summary>批量取活动范围明细（引擎匹配范围、后台编辑回显用）。</summary>
    /// <param name="activityIds">活动 ID 集合；空集合直接返回空列表（不发 SQL）。</param>
    Task<List<MarketingActivityTarget>> ListTargetsAsync(IReadOnlyCollection<long> activityIds);

    /// <summary>整组替换活动范围：先软删该活动旧范围，再批量插入新范围（调用方负责事务外的最终一致）。</summary>
    /// <param name="activityId">活动 ID。</param>
    /// <param name="targets">新的范围明细集合，允许为空（表示"全部范围"）。</param>
    Task ReplaceTargetsAsync(long activityId, IEnumerable<MarketingActivityTarget> targets);

    /// <summary>写入一条订单级参与记录及其商品行明细（两次插入，不开启事务；订单号幂等由上层保证）。</summary>
    /// <param name="record">订单级记录。</param>
    /// <param name="items">商品行明细集合。</param>
    Task InsertRecordAsync(MarketingActivityRecord record, IReadOnlyCollection<MarketingActivityRecordItem> items);

    /// <summary>按订单号取参与记录（满赠发券、取消回退、订单溯源用）。</summary>
    /// <param name="orderNo">订单号。</param>
    Task<List<MarketingActivityRecord>> ListRecordsByOrderNoAsync(string orderNo);

    /// <summary>判断订单是否已有参与记录（落账幂等：同一订单只写一次）。</summary>
    /// <param name="orderNo">订单号。</param>
    Task<bool> HasRecordAsync(string orderNo);

    /// <summary>更新满赠发放张数（发券成功后调用，幂等：发过就不再发）。</summary>
    /// <param name="recordId">订单级记录 ID。</param>
    /// <param name="giftCouponCount">最终发放张数。</param>
    Task UpdateRecordGiftCountAsync(long recordId, int giftCouponCount);

    /// <summary>按活动分页取参与记录（后台单活动明细，早期接口保留）。</summary>
    /// <param name="activityId">活动 ID。</param>
    /// <param name="from">起始时间（含），null 不过滤。</param>
    /// <param name="to">结束时间（含），null 不过滤。</param>
    /// <param name="page">页码。</param>
    /// <param name="pageSize">每页条数。</param>
    Task<List<MarketingActivityRecord>> ListRecordsAsync(long activityId, DateTime? from, DateTime? to, int page, int pageSize);

    /// <summary>报表：按租户/活动/时间分页取参与记录。</summary>
    /// <param name="platformId">平台过滤；&lt;=0 不过滤。</param>
    /// <param name="merchantId">商户过滤；&lt;=0 不过滤。</param>
    /// <param name="activityId">活动下钻过滤；&lt;=0 不过滤（看全部活动）。</param>
    /// <param name="from">起始时间（含），null 不过滤。</param>
    /// <param name="to">结束时间（含），null 不过滤。</param>
    /// <param name="page">页码。</param>
    /// <param name="pageSize">每页条数。</param>
    Task<(List<MarketingActivityRecord> Items, long Total)> QueryRecordsAsync(
        long platformId, long merchantId, long activityId, DateTime? from, DateTime? to, int page, int pageSize);

    /// <summary>报表：按活动聚合（参与订单数、折扣总额、赠券数）。数据量按区间加载后在内存分组。</summary>
    /// <param name="platformId">平台过滤；&lt;=0 不过滤。</param>
    /// <param name="merchantId">商户过滤；&lt;=0 不过滤。</param>
    /// <param name="from">起始时间（含）。</param>
    /// <param name="to">结束时间（含）。</param>
    Task<List<MarketingActivitySummaryRow>> SummarizeActivitiesAsync(long platformId, long merchantId, DateTime? from, DateTime? to);

    /// <summary>取参与记录的商品行明细；按 recordIds 或按订单号二选一（订单号优先）。</summary>
    /// <param name="recordIds">订单级记录 ID 集合；空集合且未传订单号时返回空列表。</param>
    /// <param name="orderNo">订单号；传入时忽略 recordIds，直接按订单号查全部明细。</param>
    Task<List<MarketingActivityRecordItem>> ListRecordItemsAsync(IReadOnlyCollection<long> recordIds, string? orderNo = null);

    /// <summary>取平台营销配置；没有配置时返回 null，由调用方使用默认优先级。</summary>
    /// <param name="platformId">平台 ID。</param>
    Task<MarketingConfig?> GetConfigAsync(long platformId);

    /// <summary>保存（新增或更新）平台营销配置；Id&gt;0 走更新，否则新增。</summary>
    /// <param name="config">配置实体。</param>
    Task SaveConfigAsync(MarketingConfig config);
}

/// <summary>
/// 券仓储：券模板/券活动/范围/用户券/使用记录。
/// 券活动插入与字段级更新走本接口的专用方法（泛型基类写入的是券模板表，不能复用）。
/// </summary>
public interface IMarketingCouponRepository : IBaseRepository<CouponTemplate>
{
    /// <summary>后台券模板分页。</summary>
    /// <param name="keyword">模板名称模糊匹配。</param>
    /// <param name="couponType">券类型过滤；null 不过滤。</param>
    /// <param name="platformId">平台过滤；&lt;=0 不过滤。</param>
    /// <param name="merchantId">商户过滤；&lt;=0 不过滤。</param>
    /// <param name="page">页码。</param>
    /// <param name="pageSize">每页条数。</param>
    Task<(List<CouponTemplate> Items, long Total)> QueryTemplatesPagedAsync(
        string keyword, int? couponType, long platformId, long merchantId, int page, int pageSize);

    /// <summary>后台券活动分页。</summary>
    /// <param name="keyword">券活动名称模糊匹配。</param>
    /// <param name="platformId">平台过滤；&lt;=0 不过滤。</param>
    /// <param name="merchantId">商户过滤；&lt;=0 不过滤。</param>
    /// <param name="page">页码。</param>
    /// <param name="pageSize">每页条数。</param>
    Task<(List<CouponActivity> Items, long Total)> QueryActivitiesPagedAsync(
        string keyword, long platformId, long merchantId, int page, int pageSize);

    /// <summary>按 ID 取券活动（券活动的软删除由全局过滤器处理）。</summary>
    /// <param name="id">券活动 ID。</param>
    Task<CouponActivity?> GetActivityByIdAsync(long id);

    /// <summary>新增券活动（不能走基类 InsertAsync，基类泛型是券模板）。</summary>
    /// <param name="entity">券活动实体。</param>
    Task<bool> InsertCouponActivityAsync(CouponActivity entity);

    /// <summary>更新券活动全字段（不能走基类 UpdateAsync）。</summary>
    /// <param name="entity">券活动实体。</param>
    Task<bool> UpdateCouponActivityAsync(CouponActivity entity);

    /// <summary>券活动的字段级更新（基类 UpdateColumnsAsync 会写到券模板表，必须走这个专用方法）。</summary>
    /// <param name="id">券活动 ID。</param>
    /// <param name="columns">要更新的匿名对象字段（如 new { IsEnabled, UpdatedAt }）。</param>
    Task<bool> UpdateCouponActivityColumnsAsync(long id, object columns);

    /// <summary>新增用户券实例（不能走基类 InsertAsync）。</summary>
    /// <param name="entity">用户券实体。</param>
    Task<bool> InsertUserCouponAsync(UserCoupon entity);

    /// <summary>领券中心用：平台下当前有效的启用券活动；固定按创建时间倒序（新券在前，避免堆顺序"随机排序"）。</summary>
    /// <param name="platformId">平台 ID。</param>
    /// <param name="now">当前时间，用于有效期判断。</param>
    Task<List<CouponActivity>> ListEnabledActivitiesAsync(long platformId, DateTime now);

    /// <summary>按 ID 集合取券活动（券包/结算券列表展示活动名用；不过滤启用状态，已停用活动的持有券仍可用）。</summary>
    /// <param name="ids">券活动 ID 集合；空集合返回空列表。</param>
    Task<List<CouponActivity>> ListActivitiesByIdsAsync(IReadOnlyCollection<long> ids);

    /// <summary>按 ID 集合取券模板（优惠规则来源；空集合返回空列表）。</summary>
    /// <param name="ids">模板 ID 集合。</param>
    Task<List<CouponTemplate>> ListTemplatesByIdsAsync(IReadOnlyCollection<long> ids);

    /// <summary>批量取券活动范围明细（范围匹配与后台回显用）。</summary>
    /// <param name="couponActivityIds">券活动 ID 集合；空集合返回空列表。</param>
    Task<List<CouponActivityTarget>> ListTargetsAsync(IReadOnlyCollection<long> couponActivityIds);

    /// <summary>整组替换券活动范围：先软删旧范围再批量插入新范围。</summary>
    /// <param name="couponActivityId">券活动 ID。</param>
    /// <param name="targets">新的范围明细集合。</param>
    Task ReplaceTargetsAsync(long couponActivityId, IEnumerable<CouponActivityTarget> targets);

    /// <summary>统计某券活动某用户的已发放张数（含领取与满赠来源），用于每人限领校验。</summary>
    /// <param name="couponActivityId">券活动 ID。</param>
    /// <param name="userId">用户 ID。</param>
    Task<int> CountUserIssuedAsync(long couponActivityId, long userId);

    /// <summary>用户券包查询：unusedOnly=true 时只取"未使用且未过期"，用于结算；false 取全部历史（券包页自行计算展示状态）。</summary>
    /// <param name="userId">用户 ID。</param>
    /// <param name="unusedOnly">是否只取可用券。</param>
    /// <param name="couponActivityId">可选：只看某个券活动的券；&lt;=0 不过滤。</param>
    Task<List<UserCoupon>> ListUserCouponsAsync(long userId, bool unusedOnly, long couponActivityId = 0);

    /// <summary>按用户+券 ID 集合取用户券（落账时补模板/商户快照用；自动校验归属，防越权）。</summary>
    /// <param name="userId">用户 ID。</param>
    /// <param name="couponIds">券 ID 集合。</param>
    Task<List<UserCoupon>> ListUserCouponsByIdsAsync(long userId, IReadOnlyCollection<long> couponIds);

    /// <summary>
    /// 原子占用券：仅当仍为 Unused 时条件更新为 Used 并记录订单号，返回是否成功。
    /// 并发下同一张券最多被一单占用；订单号用于取消回退与对账。
    /// </summary>
    /// <param name="userCouponId">用户券 ID。</param>
    /// <param name="orderId">订单 ID。</param>
    /// <param name="orderNo">订单号。</param>
    Task<bool> MarkUserCouponUsedAsync(long userCouponId, long orderId, string orderNo);

    /// <summary>订单取消/关单时回退占用：把被该订单号占用且仍为 Used 的券恢复为 Unused（可重复调用，无占用则无副作用）。</summary>
    /// <param name="orderNo">订单号。</param>
    Task<bool> ReleaseUserCouponAsync(string orderNo);

    /// <summary>
    /// 券活动发行量 +1（SQL 条件自增 IssuedCount&lt;TotalStock），返回是否成功；
    /// 高并发下不会超发，领取与满赠共用同一库存。
    /// </summary>
    /// <param name="couponActivityId">券活动 ID。</param>
    Task<bool> TryIncreaseIssuedAsync(long couponActivityId);

    /// <summary>写入一条订单级用券记录及其商品行明细（订单号幂等由上层保证）。</summary>
    /// <param name="record">订单级记录。</param>
    /// <param name="items">商品行明细集合。</param>
    Task InsertCouponRecordAsync(CouponRecord record, IReadOnlyCollection<CouponRecordItem> items);

    /// <summary>判断订单是否已有用券记录（落账幂等）。</summary>
    /// <param name="orderNo">订单号。</param>
    Task<bool> HasCouponRecordAsync(string orderNo);

    /// <summary>按券活动分页取用券记录（后台单活动明细，早期接口保留）。</summary>
    /// <param name="couponActivityId">券活动 ID。</param>
    /// <param name="from">起始时间（含）。</param>
    /// <param name="to">结束时间（含）。</param>
    /// <param name="page">页码。</param>
    /// <param name="pageSize">每页条数。</param>
    Task<List<CouponRecord>> ListCouponRecordsAsync(long couponActivityId, DateTime? from, DateTime? to, int page, int pageSize);

    /// <summary>报表：按租户/券活动/时间分页取用券记录。</summary>
    /// <param name="platformId">平台过滤；&lt;=0 不过滤。</param>
    /// <param name="merchantId">商户过滤；&lt;=0 不过滤。</param>
    /// <param name="couponActivityId">券活动下钻过滤；&lt;=0 不过滤。</param>
    /// <param name="from">起始时间（含）。</param>
    /// <param name="to">结束时间（含）。</param>
    /// <param name="page">页码。</param>
    /// <param name="pageSize">每页条数。</param>
    Task<(List<CouponRecord> Items, long Total)> QueryCouponRecordsAsync(
        long platformId, long merchantId, long couponActivityId, DateTime? from, DateTime? to, int page, int pageSize);

    /// <summary>报表：按券活动聚合（核销笔数、抵扣总额）。数据量按区间加载后在内存分组。</summary>
    /// <param name="platformId">平台过滤；&lt;=0 不过滤。</param>
    /// <param name="merchantId">商户过滤；&lt;=0 不过滤。</param>
    /// <param name="from">起始时间（含）。</param>
    /// <param name="to">结束时间（含）。</param>
    Task<List<CouponSummaryRow>> SummarizeCouponsAsync(long platformId, long merchantId, DateTime? from, DateTime? to);

    /// <summary>取用券记录的商品行明细；按 recordIds 或按订单号二选一（订单号优先）。</summary>
    /// <param name="recordIds">订单级记录 ID 集合。</param>
    /// <param name="orderNo">订单号；传入时忽略 recordIds。</param>
    Task<List<CouponRecordItem>> ListCouponRecordItemsAsync(IReadOnlyCollection<long> recordIds, string? orderNo = null);
}
