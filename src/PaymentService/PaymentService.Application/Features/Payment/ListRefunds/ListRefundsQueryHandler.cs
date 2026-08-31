using CommunalService.Domain;
using CommunalService.Domain.Enums;
using MediatR;
using PaymentService.Domain.IRepository;

namespace PaymentService.Application.Features.Payment.ListRefunds;

/// <summary>
/// 退款单分页列表：keyword 匹配退款单号/业务单号；租户裁剪同支付列表。
/// </summary>
public class ListRefundsQueryHandler(IPaymentOrderRepository repository, TenantContext tenant)
    : IRequestHandler<ListRefundsQuery, ApiResponse>
{
    public async Task<ApiResponse> Handle(ListRefundsQuery request, CancellationToken cancellationToken)
    {
        if (!tenant.HasWildcard && !tenant.IsPlatform && !tenant.IsMerchant && !tenant.IsCustomer)
            return ApiResults.Fail(BaseApiResponseCode.Unauthorized, "请先登录");

        var platformId = tenant.IsPlatform ? tenant.PlatformId : (long?)null;
        var merchantId = tenant.IsMerchant ? tenant.MerchantId : (long?)null;
        var scopedUserId = tenant.IsCustomer ? tenant.UserId : request.UserId;
        var (items, total) = await repository.QueryRefundsPagedAsync(
            request.Keyword, request.Status, scopedUserId, platformId, merchantId,
            request.Page, request.PageSize, cancellationToken);
        return ApiResults.Ok(new { items, total, page = request.Page, pageSize = request.PageSize });
    }
}
