using CommunalService.Domain;
using CommunalService.Domain.Enums;
using MediatR;
using EntityProduct = ProductService.Domain.Entity.Product;
using ProductService.Domain.Entity;
using ProductService.Domain.IRepository;

namespace ProductService.Application.Features.Product.PublishProduct;

public class PublishProductCommandHandler(
    IProductRepository<EntityProduct> repository,
    ISkuRepository<Sku> skuRepository,
    TenantContext tenant)
    : IRequestHandler<PublishProductCommand, ApiResponse>
{
    public async Task<ApiResponse> Handle(PublishProductCommand request, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(request.Id);
        if (product == null
            || (tenant.IsPlatform && product.PlatformId != tenant.PlatformId)
            || (tenant.IsMerchant && product.MerchantId != tenant.MerchantId))
            return ApiResults.Fail(BaseApiResponseCode.Forbidden, "无权操作该商品");

        // 归属以商品事实回填，避免跨商户指定。
        request.MerchantId = product.MerchantId;

        if (!request.Approved)
        {
            product.ReviewStatus = 2;
            product.Status = 2;
            await repository.UpdateAsync(product);
            return ApiResults.Ok(new { success = true, reviewStatus = product.ReviewStatus, status = product.Status });
        }

        // 审核与上架一次完成：业务员点击“审核上架”后商品必须立刻进入可售状态。
        product.ReviewStatus = 1;
        var skus = await skuRepository.QueryAsync(s => s.ProductId == request.Id && s.IsActive && s.Stock > 0);
        if (skus.Count == 0)
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "商品没有可售SKU，无法上架");

        product.Status = 1;
        await repository.UpdateAsync(product);
        return ApiResults.Ok(new { success = true, reviewStatus = product.ReviewStatus, status = product.Status });
    }
}
