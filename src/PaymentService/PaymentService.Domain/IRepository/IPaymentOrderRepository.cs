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

    // 退款审批与查询所需；实现在 Infrastructure，避免 Application 直接依赖 IFreeSql。
    Task<RefundOrder?> GetRefundByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<List<RefundOrderItem>> ListRefundItemsAsync(long refundId, CancellationToken cancellationToken = default);
    Task<List<RefundOrder>> ListRefundsForBizAsync(string bizNo, CancellationToken cancellationToken = default);
    Task<(List<RefundOrder> Items, long Total)> QueryRefundsPagedAsync(string keyword, int? status, long userId,
        long? platformId, long? merchantId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<(List<PaymentOrder> Items, long Total)> QueryPaymentsPagedAsync(string keyword, int? status, long userId,
        long? platformId, long? merchantId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<bool> MarkRefundRejectedAsync(long refundId, string reason, CancellationToken cancellationToken = default);
    Task<bool> AddRefundItemsAsync(long refundId, string bizNo, List<(long SkuId, int Quantity)> items, CancellationToken cancellationToken = default);
}
