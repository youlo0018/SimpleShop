using FreeSql;
using PaymentService.Domain.Entity;
using PaymentService.Domain.IRepository;

namespace PaymentService.Infrastructure.Repository;

public class PaymentOrderRepository(IFreeSql freeSql) : IPaymentOrderRepository
{
    public Task<PaymentOrder?> GetByBizNoAsync(string bizNo, CancellationToken cancellationToken = default)
        => freeSql.Select<PaymentOrder>().Where(payment => payment.BizNo == bizNo).FirstAsync();

    public Task<PaymentOrder?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => freeSql.Select<PaymentOrder>().Where(payment => payment.Id == id).FirstAsync();

    public async Task<bool> AddAsync(PaymentOrder payment, CancellationToken cancellationToken = default)
        => await freeSql.Insert(payment).ExecuteAffrowsAsync(cancellationToken) > 0;

    public Task<RefundOrder?> GetRefundByNoAsync(string refundNo, CancellationToken cancellationToken = default)
        => freeSql.Select<RefundOrder>().Where(refund => refund.RefundNo == refundNo).FirstAsync(cancellationToken);

    public async Task<bool> AddRefundAsync(RefundOrder refund, CancellationToken cancellationToken = default)
        => await freeSql.Insert(refund).ExecuteAffrowsAsync(cancellationToken) > 0;

    public Task<decimal> GetRefundedAmountAsync(long paymentId, CancellationToken cancellationToken = default)
        => freeSql.Select<RefundOrder>()
            .Where(refund => refund.PaymentId == paymentId && refund.Status == 20)
            .SumAsync(refund => refund.Amount, cancellationToken);

    public Task<decimal> GetCommittedRefundAmountAsync(long paymentId, CancellationToken cancellationToken = default)
        => freeSql.Select<RefundOrder>()
            .Where(refund => refund.PaymentId == paymentId && (refund.Status == 10 || refund.Status == 20))
            .SumAsync(refund => refund.Amount, cancellationToken);

    public async Task<bool> MarkRefundedAsync(string refundNo, CancellationToken cancellationToken = default)
        => await freeSql.Update<RefundOrder>()
            .Where(refund => refund.RefundNo == refundNo && refund.Status == 10)
            .Set(refund => refund.Status, 20)
            .Set(refund => refund.RefundedAt, DateTime.Now)
            .ExecuteAffrowsAsync(cancellationToken) > 0;

    public async Task<bool> MarkPaidAsync(long id, CancellationToken cancellationToken = default)
        => await freeSql.Update<PaymentOrder>()
            .Where(payment => payment.Id == id && payment.Status == 10)
            .Set(payment => payment.Status, 20)
            .Set(payment => payment.PaidAt, DateTime.Now)
            .ExecuteAffrowsAsync(cancellationToken) > 0;

    public Task<RefundOrder?> GetRefundByIdAsync(long id, CancellationToken cancellationToken = default)
        => freeSql.Select<RefundOrder>().Where(refund => refund.Id == id && !refund.IsDeleted).FirstAsync();

    public Task<List<RefundOrderItem>> ListRefundItemsAsync(long refundId, CancellationToken cancellationToken = default)
        => freeSql.Select<RefundOrderItem>().Where(item => item.RefundId == refundId).ToListAsync();

    public Task<List<RefundOrder>> ListRefundsForBizAsync(string bizNo, CancellationToken cancellationToken = default)
        => freeSql.Select<RefundOrder>().Where(item => item.BizNo == bizNo && !item.IsDeleted)
            .OrderByDescending(item => item.CreatedAt).ToListAsync();

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

    public async Task<bool> MarkRefundRejectedAsync(long refundId, string reason, CancellationToken cancellationToken = default)
        => await freeSql.Update<RefundOrder>().Where(item => item.Id == refundId)
            .Set(item => item.Status, 90)
            .Set(item => item.Reason, reason)
            .ExecuteAffrowsAsync() > 0;

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
