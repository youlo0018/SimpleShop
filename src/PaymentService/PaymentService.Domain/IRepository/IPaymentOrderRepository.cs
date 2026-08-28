using PaymentService.Domain.Entity;

namespace PaymentService.Domain.IRepository;

public interface IPaymentOrderRepository
{
    Task<PaymentOrder?> GetByBizNoAsync(string bizNo, CancellationToken cancellationToken = default);
    Task<PaymentOrder?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<bool> AddAsync(PaymentOrder payment, CancellationToken cancellationToken = default);
    Task<bool> MarkPaidAsync(long id, CancellationToken cancellationToken = default);
    Task<RefundOrder?> GetRefundByNoAsync(string refundNo, CancellationToken cancellationToken = default);
    Task<bool> AddRefundAsync(RefundOrder refund, CancellationToken cancellationToken = default);
    Task<decimal> GetRefundedAmountAsync(long paymentId, CancellationToken cancellationToken = default);
    Task<decimal> GetCommittedRefundAmountAsync(long paymentId, CancellationToken cancellationToken = default);
    Task<bool> MarkRefundedAsync(string refundNo, CancellationToken cancellationToken = default);
}
