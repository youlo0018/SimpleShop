using CommunalService.Domain;
using CommunalService.Domain.Enums;
using MediatR;
using OrderService.Domain.IRepository;

namespace OrderService.Application.Features.GetOrderList;

/// <summary>
/// 订单分页列表：keyword 匹配订单号/收货人/电话；租户裁剪（平台→本平台、商户→本商户、客户→本人订单）。
/// total 与取数同条件（FreeSql Page 第一参数是页码，不是偏移量）。
/// </summary>
public class ListOrdersQueryHandler(IOrderRepository repository, TenantContext tenant)
    : IRequestHandler<ListOrdersQuery, ApiResponse>
{
    /// <summary>处理入口：订单分页列表：keyword 匹配订单号/收货人/电话；租户裁剪（平台→本平台、商户→本商户、客户→本人订单）。 total 与取数同条件（FreeSql Page 第一参数是页码，不是偏移量）。</summary>
    public async Task<ApiResponse> Handle(ListOrdersQuery request, CancellationToken cancellationToken)
    {
        if (!tenant.HasWildcard && !tenant.IsPlatform && !tenant.IsMerchant && !tenant.IsCustomer)
            return ApiResults.Fail(BaseApiResponseCode.Unauthorized, "请先登录");

        // 租户裁剪：平台看本平台、商户看本商户、客户只看自己的订单。
        var platformId = tenant.IsPlatform ? tenant.PlatformId : (long?)null;
        var merchantId = tenant.IsMerchant ? tenant.MerchantId : (long?)null;
        var customerId = tenant.IsCustomer ? tenant.UserId : request.CustomerId;

        var (items, total) = await repository.QueryPagedAsync(
            request.Keyword, request.Status, customerId, platformId, merchantId,
            request.Page, request.PageSize, cancellationToken);
        return ApiResults.Ok(new { items, total, page = request.Page, pageSize = request.PageSize });
    }
}
