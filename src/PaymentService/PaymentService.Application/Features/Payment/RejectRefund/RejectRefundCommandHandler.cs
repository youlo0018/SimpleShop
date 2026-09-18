using CommunalService.Domain;
using CommunalService.Domain.Enums;
using MediatR;
using PaymentService.Domain.IRepository;

namespace PaymentService.Application.Features.Payment.RejectRefund;

/// <summary>
/// 拒绝退款：条件更新退款单状态为 90 并记录原因；不发事件、不动库存与订单。
/// </summary>
public class RejectRefundCommandHandler(IPaymentOrderRepository repository, TenantContext tenant)
    : IRequestHandler<RejectRefundCommand, ApiResponse>
{
    /// <summary>处理入口：拒绝退款：条件更新退款单状态为 90 并记录原因；不发事件、不动库存与订单。</summary>
    public async Task<ApiResponse> Handle(RejectRefundCommand request, CancellationToken cancellationToken)
    {
        var refund = await repository.GetRefundByIdAsync(request.Id, cancellationToken);
        if (refund is null) return ApiResults.Fail(BaseApiResponseCode.NotFound, "退款单不存在");
        if (!CanDecide(refund)) return ApiResults.Fail(BaseApiResponseCode.Forbidden, "无权审批该退款单");
        if (refund.Status != 10) return ApiResults.Fail(BaseApiResponseCode.BadRequest, "当前退款状态不可审批");

        var updated = await repository.MarkRefundRejectedAsync(
            refund.Id, string.IsNullOrWhiteSpace(request.Reason) ? "审核不通过" : request.Reason, cancellationToken);
        return updated
            ? ApiResults.Ok(new { success = true })
            : ApiResults.Fail(BaseApiResponseCode.BadRequest, "退款状态更新失败");
    }

    /// <summary>条件判断：CanDecide。</summary>
    private bool CanDecide(Domain.Entity.RefundOrder refund)
        => tenant.HasWildcard || tenant.IsPlatform || (tenant.IsMerchant && refund.MerchantId == tenant.MerchantId);
}
