using CommunalService.Domain.Logging;
using CommunalService.Domain.Messaging;
using Microsoft.AspNetCore.Http;
using MediatR;
using EntityProduct = ProductService.Domain.Entity.Product;
using ProductService.Domain.Entity;
using ProductService.Domain.IRepository;

namespace ProductService.Application.Features.Product.CreateProduct;

public class CreateProductCommandHandler(
    IProductRepository<EntityProduct> productRepository,
    ISkuRepository<Sku> skuRepository,
    IMessagePublisher messagePublisher,
    IHttpContextAccessor httpContextAccessor,
    IOperationLogger operationLogger)
    : IRequestHandler<CreateProductCommand, object>
{
    public async Task<object> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
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

        return product.Id;
    }
}
