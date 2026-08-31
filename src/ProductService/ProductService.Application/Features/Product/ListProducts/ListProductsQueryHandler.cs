using CommunalService.Domain;
using MediatR;
using ProductService.Domain.IRepository;

namespace ProductService.Application.Features.Product.ListProducts;

/// <summary>
/// 商品分页列表：keyword/分类/商户/状态过滤；租户裁剪（平台→本平台、商户→本商户）；
/// 一次批量查 SKU 后内存分组，避免 N+1 查询。
/// </summary>
public class ListProductsQueryHandler(IProductAdminRepository repository, TenantContext tenant)
    : IRequestHandler<ListProductsQuery, ApiResponse>
{
    public async Task<ApiResponse> Handle(ListProductsQuery request, CancellationToken cancellationToken)
    {
        var (products, total) = await repository.QueryProductsPagedAsync(
            request.Keyword, request.CategoryId, request.MerchantId,
            tenant.IsPlatform ? tenant.PlatformId : null,
            tenant.IsMerchant ? tenant.MerchantId : null,
            request.Status, request.Page, request.PageSize, cancellationToken);

        var ids = products.Select(product => product.Id).ToArray();
        var skus = await repository.ListSkusByProductIdsAsync(ids, cancellationToken);

        return ApiResults.Ok(new
        {
            items = products.Select(product => new
            {
                product.Id,
                product.Name,
                product.MainImage,
                product.Description,
                product.CategoryId,
                product.MerchantId,
                product.Status,
                product.ReviewStatus,
                skus = skus.Where(sku => sku.ProductId == product.Id)
            }),
            total,
            page = request.Page,
            pageSize = request.PageSize
        });
    }
}
