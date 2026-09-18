using FreeSql;
using PaymentService.Domain.Entity;
using PaymentService.Domain.IRepository;

namespace PaymentService.Infrastructure.Repository;

/// <summary>支付/退款仓储实现：支付单与退款单读写、退款累计金额统计与状态条件更新。</summary>
    public class PaymentOrderRepository(IFreeSql freeSql) : IPaymentOrderRepository
{
    /// <summary>查询：GetByBizNoAsync。</summary>
    /// <summary>按业务单号取支付单（创建支付幂等）。</summary>
    public Task<PaymentOrder?> GetByBizNoAsync(string bizNo, CancellationToken cancellationToken = default)
        => freeSql.Select<PaymentOrder>().Where(payment => payment.BizNo == bizNo).FirstAsync();

    /// <summary>查询：GetByIdAsync。</summary>
    /// <summary>按主键取支付单。</summary>
    public Task<PaymentOrder?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => freeSql.Select<PaymentOrder>().Where(payment => payment.Id == id).FirstAsync();

    /// <summary>写入/新增：AddAsync。</summary>
    /// <summary>新增支付单（BizNo 唯一）。</summary>
    public async Task<bool> AddAsync(PaymentOrder payment, CancellationToken cancellationToken = default)
        => await freeSql.Insert(payment).ExecuteAffrowsAsync(cancellationToken) > 0;

    /// <summary>查询：GetRefundByNoAsync。</summary>
    /// <summary>按退款单号取退款单。</summary>
    public Task<RefundOrder?> GetRefundByNoAsync(string refundNo, CancellationToken cancellationToken = default)
        => freeSql.Select<RefundOrder>().Where(refund => refund.RefundNo == refundNo).FirstAsync(cancellationToken);

    /// <summary>写入/新增：AddRefundAsync。</summary>
    /// <summary>新增退款单（含明细）。</summary>
    public async Task<bool> AddRefundAsync(RefundOrder refund, CancellationToken cancellationToken = default)
        => await freeSql.Insert(refund).ExecuteAffrowsAsync(cancellationToken) > 0;

    /// <summary>查询：GetRefundedAmountAsync。</summary>
    /// <summary>已成功退款累计金额（退款限额校验）。</summary>
    public Task<decimal> GetRefundedAmountAsync(long paymentId, CancellationToken cancellationToken = default)
        => freeSql.Select<RefundOrder>()
            .Where(refund => refund.PaymentId == paymentId && refund.Status == 20)
            .SumAsync(refund => refund.Amount, cancellationToken);

    /// <summary>查询：GetCommittedRefundAmountAsync。</summary>
    /// <summary>已申请/已通过退款累计金额（含待审批，防超额申请）。</summary>
    public Task<decimal> GetCommittedRefundAmountAsync(long paymentId, CancellationToken cancellationToken = default)
        => freeSql.Select<RefundOrder>()
            .Where(refund => refund.PaymentId == paymentId && (refund.Status == 10 || refund.Status == 20))
            .SumAsync(refund => refund.Amount, cancellationToken);

    /// <summary>条件标记退款单已退款（幂等）。</summary>
    public async Task<bool> MarkRefundedAsync(string refundNo, CancellationToken cancellationToken = default)
        => await freeSql.Update<RefundOrder>()
            .Where(refund => refund.RefundNo == refundNo && refund.Status == 10)
            .Set(refund => refund.Status, 20)
            .Set(refund => refund.RefundedAt, DateTime.Now)
            .ExecuteAffrowsAsync(cancellationToken) > 0;

    /// <summary>条件标记支付单已支付（幂等）。</summary>
    public async Task<bool> MarkPaidAsync(long id, CancellationToken cancellationToken = default)
        => await freeSql.Update<PaymentOrder>()
            .Where(payment => payment.Id == id && payment.Status == 10)
            .Set(payment => payment.Status, 20)
            .Set(payment => payment.PaidAt, DateTime.Now)
            .ExecuteAffrowsAsync(cancellationToken) > 0;

    /// <summary>查询：GetRefundByIdAsync。</summary>
    /// <summary>按主键取退款单。</summary>
    public Task<RefundOrder?> GetRefundByIdAsync(long id, CancellationToken cancellationToken = default)
        => freeSql.Select<RefundOrder>().Where(refund => refund.Id == id && !refund.IsDeleted).FirstAsync();

    /// <summary>查询列表（分页语义由实现约定）：ListRefundItemsAsync。</summary>
    /// <summary>取退款明细（库存回补/审批展示用）。</summary>
    public Task<List<RefundOrderItem>> ListRefundItemsAsync(long refundId, CancellationToken cancellationToken = default)
        => freeSql.Select<RefundOrderItem>().Where(item => item.RefundId == refundId).ToListAsync();

    /// <summary>查询列表（分页语义由实现约定）：ListRefundsForBizAsync。</summary>
    /// <summary>按业务单号取全部退款单（订单退款状态汇总）。</summary>
    public Task<List<RefundOrder>> ListRefundsForBizAsync(string bizNo, CancellationToken cancellationToken = default)
        => freeSql.Select<RefundOrder>().Where(item => item.BizNo == bizNo && !item.IsDeleted)
            .OrderByDescending(item => item.CreatedAt).ToListAsync();

    /// <summary>后台退款分页查询：关键字/状态/用户过滤。</summary>
    public async Task<(List<RefundOrder> Items, long Total)> QueryRefundsPagedAsync(string keyword, int? status, long userId,
        long? platformId, long? merchantId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var selection = freeSql.Select<RefundOrder>()
            .Where(refund => !refund.IsDeleted)
            .WhereIf(!string.IsNullOrWhiteSpace(keyword), refund =>
                refund.RefundNo.Contains(keyword) || refund.BizNo.Contains(keyword))
            .WhereIf(status.HasValue, refund => refund.Status == status!.Value)
            .WhereIf(userId > 0, refund => refund.UserId == userId)
            .WhereIf(platformId.HasValue, refund => refund.PlatformId == platformId!.Value)
            .WhereIf(merchantId.HasValue, refund => refund.MerchantId == merchantId!.Value);
        var total = await selection.CountAsync();
        var items = await selection.OrderByDescending(refund => refund.CreatedAt)
            .Page(Math.Max(page, 1), pageSize).ToListAsync();
        return (items, total);
    }

    /// <summary>后台支付分页查询：关键字/状态/用户过滤。</summary>
    public async Task<(List<PaymentOrder> Items, long Total)> QueryPaymentsPagedAsync(string keyword, int? status, long userId,
        long? platformId, long? merchantId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var selection = freeSql.Select<PaymentOrder>()
            .Where(payment => !payment.IsDeleted)
            .WhereIf(!string.IsNullOrWhiteSpace(keyword), payment =>
                payment.PaymentNo.Contains(keyword) || payment.BizNo.Contains(keyword))
            .WhereIf(status.HasValue, payment => payment.Status == status!.Value)
            .WhereIf(userId > 0, payment => payment.UserId == userId)
            .WhereIf(platformId.HasValue, payment => payment.PlatformId == platformId!.Value)
            .WhereIf(merchantId.HasValue, payment => payment.MerchantId == merchantId!.Value);
        var total = await selection.CountAsync();
        var items = await selection.OrderByDescending(payment => payment.CreatedAt)
            .Page(Math.Max(page, 1), pageSize).ToListAsync();
        return (items, total);
    }

    /// <summary>条件标记退款单已拒绝并记录原因。</summary>
    public async Task<bool> MarkRefundRejectedAsync(long refundId, string reason, CancellationToken cancellationToken = default)
        => await freeSql.Update<RefundOrder>().Where(item => item.Id == refundId)
            .Set(item => item.Status, 90)
            .Set(item => item.Reason, reason)
            .ExecuteAffrowsAsync() > 0;

    /// <summary>写入/新增：AddRefundItemsAsync。</summary>
    public async Task<bool> AddRefundItemsAsync(long refundId, string bizNo, List<(long SkuId, int Quantity)> items, CancellationToken cancellationToken = default)
    {
        var refundItems = items.Select(item => new RefundOrderItem
        {
            RefundId = refundId,
            BizNo = bizNo,
            SkuId = item.SkuId,
            Quantity = item.Quantity
        }).ToList();
        return await freeSql.Insert(refundItems).ExecuteAffrowsAsync(cancellationToken) > 0;
    }
}
