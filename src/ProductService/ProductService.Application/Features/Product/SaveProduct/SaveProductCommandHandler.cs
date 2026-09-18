using CommunalService.Domain;
using CommunalService.Domain.Enums;
using MediatR;
using ProductService.Domain.Entity;
using ProductService.Domain.IRepository;

namespace ProductService.Application.Features.Product.SaveProduct;

/// <summary>
/// 商品编辑保存：主表更新 + SKU 按编码 Upsert（新增插入、已存在更新；价格/库存非法的 SKU 跳过）。
/// 写入路径收口在 IProductAdminRepository.UpdateProductWithSkusAsync。
/// </summary>
public class SaveProductCommandHandler(IProductAdminRepository repository, TenantContext tenant)
    : IRequestHandler<SaveProductCommand, ApiResponse>
{
    /// <summary>处理入口：商品编辑保存：主表更新 + SKU 按编码 Upsert（新增插入、已存在更新；价格/库存非法的 SKU 跳过）。 写入路径收口在 IProductAdminRepository.UpdateProductWithSkusAsync。</summary>
    public async Task<ApiResponse> Handle(SaveProductCommand request, CancellationToken cancellationToken)
    {
        var product = await repository.GetProductInScopeAsync(
            request.Id,
            tenant.IsPlatform ? tenant.PlatformId : null,
            tenant.IsMerchant ? tenant.MerchantId : null,
            cancellationToken);
        if (product is null)
            return ApiResults.Fail(BaseApiResponseCode.NotFound, "商品不存在");

        product.Name = request.Name;
        product.MainImage = request.MainImage;
        product.Description = request.Description;
        product.CategoryId = request.CategoryId;

        await repository.UpdateProductWithSkusAsync(product,
            request.Skus.Select(sku => (sku.SkuCode, sku.Price, sku.OriginalPrice, sku.Stock, sku.Image, sku.SpecName, sku.SpecValue)).ToList(),
            cancellationToken);
        return ApiResults.Ok(new { success = true });
    }
}
