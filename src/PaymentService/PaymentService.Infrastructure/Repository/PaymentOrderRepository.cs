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
}
