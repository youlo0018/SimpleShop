using CommunalService.Domain;
using CommunalService.Domain.Enums;
using MediatR;
using EntityProduct = ProductService.Domain.Entity.Product;
using ProductService.Domain.IRepository;

namespace ProductService.Application.Features.Product.OffShelfProduct;

/// <summary>
/// 商品下架：Status 置 2；租户裁剪（平台→本平台、商户→本商户），无权时 403。
/// </summary>
public class OffShelfProductCommandHandler(IProductAdminRepository repository, TenantContext tenant)
    : IRequestHandler<OffShelfProductCommand, ApiResponse>
{
    public async Task<ApiResponse> Handle(OffShelfProductCommand request, CancellationToken cancellationToken)
    {
        var product = await repository.GetProductInScopeAsync(
            request.Id,
            tenant.IsPlatform ? tenant.PlatformId : null,
            tenant.IsMerchant ? tenant.MerchantId : null,
            cancellationToken);
        if (product is null)
            return ApiResults.Fail(BaseApiResponseCode.Forbidden, "无权操作该商品");

        product.Status = 2;
        await repository.UpdateProductWithSkusAsync(product, [], cancellationToken);
        return ApiResults.Ok(new { success = true });
    }
}
