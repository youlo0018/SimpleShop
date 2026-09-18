using MediatR;
using EntityProduct = ProductService.Domain.Entity.Product;
using ProductService.Domain.Entity;
using ProductService.Domain.IRepository;

namespace ProductService.Application.Features.Product.GetProductDetail;

/// <summary>商品详情：返回商品与启用 SKU（游客可见）。</summary>
public class GetProductDetailCommandHandler(
    IProductRepository<EntityProduct> productRepository,
    ISkuRepository<Sku> skuRepository)
    : IRequestHandler<GetProductDetailCommand, object>
{
    /// <summary>处理入口：商品详情：返回商品与启用 SKU（游客可见）。</summary>
    public async Task<object> Handle(GetProductDetailCommand request, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(request.Id);
        if (product == null)
            return null;

        if (product.Status != 1 || product.ReviewStatus != 1)
            return new { visible = false };

        var skus = await skuRepository.QueryAsync(s => s.ProductId == request.Id && s.IsActive);
        return new { product, skus };
    }
}
