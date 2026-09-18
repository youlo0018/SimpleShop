using CommunalService.Domain;
using CommunalService.Domain.Enums;
using CommunalService.Domain.Logging;
using CommunalService.Domain.Messaging;
using Microsoft.AspNetCore.Http;
using MediatR;
using EntityProduct = ProductService.Domain.Entity.Product;
using ProductService.Domain.Entity;
using ProductService.Domain.IRepository;

namespace ProductService.Application.Features.Product.CreateProduct;

/// <summary>新建商品：SKU 校验/编码查重 → 落库（按租户确定审核状态）→ 发 product.created 建库存。</summary>
public class CreateProductCommandHandler(
    IProductRepository<EntityProduct> productRepository,
    ISkuRepository<Sku> skuRepository,
    IProductAdminRepository adminRepository,
    TenantContext tenant,
    IMessagePublisher messagePublisher,
    IHttpContextAccessor httpContextAccessor,
    IOperationLogger operationLogger)
    : IRequestHandler<CreateProductCommand, ApiResponse>
{
    /// <summary>处理入口：新建商品：SKU 校验/编码查重 → 落库（按租户确定审核状态）→ 发 product.created 建库存。</summary>
    public async Task<ApiResponse> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        // 租户归属回填与库内唯一性/存在性校验；字段级校验见 CreateProductValidator。
        request.PlatformId = tenant.IsPlatform ? tenant.PlatformId : request.PlatformId;
        request.MerchantId = tenant.IsMerchant ? tenant.MerchantId : request.MerchantId;
        if (request.MerchantId <= 0) return ApiResults.Fail(BaseApiResponseCode.BadRequest, "商户必选");
        if (request.PlatformId <= 0) return ApiResults.Fail(BaseApiResponseCode.BadRequest, "平台归属缺失");
        var category = await adminRepository.GetCategoryAsync(request.CategoryId, cancellationToken);
        if (category is null || !category.IsActive || category.IsDeleted)
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "分类不存在或已停用");
        if (await adminRepository.SkuCodesExistAsync(request.Skus.Select(sku => sku.SkuCode), cancellationToken))
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "SKU编码已存在");

        var product = request.ToProduct();
        await productRepository.InsertAsync(product);

        var skus = request.Skus.Select(s => s.ToSku(request, product.Id)).ToList();
        foreach (var sku in skus)
            await skuRepository.InsertAsync(sku);

        // 商品创建后必须同步初始化库存；否则交易服务只能依赖手工补数。
        await messagePublisher.PublishAsync(
            "product.created",
            $"product:{product.Id}",
            new MessageEnvelope<object>(
                Guid.NewGuid(),
                "product.created",
                DateTimeOffset.UtcNow,
                Guid.NewGuid().ToString("N"),
                product.PlatformId,
                product.MerchantId,
                0,
                1,
                new
                {
                    productId = product.Id,
                    platformId = product.PlatformId,
                    merchantId = product.MerchantId,
                    skuItems = skus.Select(sku => new { skuId = sku.Id, quantity = sku.Stock })
                }),
            cancellationToken);

        // 商品是交易入口数据，创建动作需要能追溯到操作人和来源平台/商户。
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            await operationLogger.LogAsync(
                httpContext,
                "create",
                "product",
                product.Id.ToString(),
                $"创建商品：{request.Name}，SKU 数量：{skus.Count}",
                cancellationToken);
        }

        return ApiResults.Ok(product.Id);
    }
}
