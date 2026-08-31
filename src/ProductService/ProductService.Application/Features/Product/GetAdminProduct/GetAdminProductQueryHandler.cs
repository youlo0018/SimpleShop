using CommunalService.Domain;
using CommunalService.Domain.Enums;
using MediatR;
using ProductService.Domain.IRepository;

namespace ProductService.Application.Features.Product.GetAdminProduct;

/// <summary>
/// 后台商品详情：商品 + 全部 SKU；按租户裁剪可见范围（无权时与不存在同样返回 404，不泄露存在性）。
/// </summary>
public class GetAdminProductQueryHandler(IProductAdminRepository repository, TenantContext tenant)
    : IRequestHandler<GetAdminProductQuery, ApiResponse>
{
    public async Task<ApiResponse> Handle(GetAdminProductQuery request, CancellationToken cancellationToken)
    {
        var product = await repository.GetProductInScopeAsync(
            request.Id,
            tenant.IsPlatform ? tenant.PlatformId : null,
            tenant.IsMerchant ? tenant.MerchantId : null,
            cancellationToken);
        if (product is null)
            return ApiResults.Fail(BaseApiResponseCode.NotFound, "商品不存在");

        return ApiResults.Ok(new
        {
            product,
            skus = await repository.ListSkusByProductIdsAsync([product.Id], cancellationToken)
        });
    }
}
