using CommunalService.Domain;
using CommunalService.Domain.Enums;
using MediatR;
using PaymentService.Domain.IRepository;

namespace PaymentService.Application.Features.Payment.RefundDetail;

/// <summary>
/// 退款详情：退款单 + 退款商品明细 + 同一业务单号下的历史退款（用于累计展示）；按租户裁剪可见范围。
/// </summary>
public class RefundDetailQueryHandler(IPaymentOrderRepository repository, TenantContext tenant)
    : IRequestHandler<RefundDetailQuery, ApiResponse>
{
    /// <summary>处理入口：退款详情：退款单 + 退款商品明细 + 同一业务单号下的历史退款（用于累计展示）；按租户裁剪可见范围。</summary>
    public async Task<ApiResponse> Handle(RefundDetailQuery request, CancellationToken cancellationToken)
    {
        var refund = await repository.GetRefundByIdAsync(request.Id, cancellationToken);
        if (refund is null || OutOfScope(refund))
            return ApiResults.Fail(BaseApiResponseCode.NotFound, "退款单不存在");

        return ApiResults.Ok(new
        {
            refund,
            items = await repository.ListRefundItemsAsync(refund.Id, cancellationToken),
            allRefunds = await repository.ListRefundsForBizAsync(refund.BizNo, cancellationToken)
        });
    }

    /// <summary>内部处理：OutOfScope。</summary>
    private bool OutOfScope(Domain.Entity.RefundOrder refund)
        => (tenant.IsPlatform && refund.PlatformId != tenant.PlatformId)
           || (tenant.IsMerchant && refund.MerchantId != tenant.MerchantId)
           || (tenant.IsCustomer && refund.UserId != tenant.UserId);
}
