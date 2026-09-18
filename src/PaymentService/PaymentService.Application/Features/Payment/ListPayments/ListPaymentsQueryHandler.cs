using CommunalService.Domain;
using CommunalService.Domain.Enums;
using MediatR;
using PaymentService.Domain.IRepository;

namespace PaymentService.Application.Features.Payment.ListPayments;

/// <summary>
/// 支付单分页列表：keyword 匹配支付单号/业务单号；租户裁剪（平台→本平台、商户→本商户、客户→本人）。
/// </summary>
public class ListPaymentsQueryHandler(IPaymentOrderRepository repository, TenantContext tenant)
    : IRequestHandler<ListPaymentsQuery, ApiResponse>
{
    /// <summary>处理入口：支付单分页列表：keyword 匹配支付单号/业务单号；租户裁剪（平台→本平台、商户→本商户、客户→本人）。</summary>
    public async Task<ApiResponse> Handle(ListPaymentsQuery request, CancellationToken cancellationToken)
    {
        if (!tenant.HasWildcard && !tenant.IsPlatform && !tenant.IsMerchant && !tenant.IsCustomer)
            return ApiResults.Fail(BaseApiResponseCode.Unauthorized, "请先登录");

        var (platformId, merchantId, scopedUserId) = ResolveScope(request.UserId);
        var (items, total) = await repository.QueryPaymentsPagedAsync(
            request.Keyword, request.Status, scopedUserId, platformId, merchantId,
            request.Page, request.PageSize, cancellationToken);
        return ApiResults.Ok(new { items, total, page = request.Page, pageSize = request.PageSize });
    }

    private (long? PlatformId, long? MerchantId, long UserId) ResolveScope(long requestedUserId)
        => (
            tenant.IsPlatform ? tenant.PlatformId : null,
            tenant.IsMerchant ? tenant.MerchantId : null,
            tenant.IsCustomer ? tenant.UserId : requestedUserId);
}
