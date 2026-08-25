using MediatR;
using EntityProduct = ProductService.Domain.Entity.Product;
using ProductService.Domain.Entity;
using ProductService.Domain.IRepository;

namespace ProductService.Application.Features.Product.GetProductDetail;

public class GetProductDetailCommandHandler(
    IProductRepository<EntityProduct> productRepository,
    ISkuRepository<Sku> skuRepository)
    : IRequestHandler<GetProductDetailCommand, object>
{
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
