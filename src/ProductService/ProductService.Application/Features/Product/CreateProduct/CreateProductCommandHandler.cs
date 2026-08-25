using MediatR;
using EntityProduct = ProductService.Domain.Entity.Product;
using ProductService.Domain.Entity;
using ProductService.Domain.IRepository;

namespace ProductService.Application.Features.Product.CreateProduct;

public class CreateProductCommandHandler(
    IProductRepository<EntityProduct> productRepository,
    ISkuRepository<Sku> skuRepository)
    : IRequestHandler<CreateProductCommand, object>
{
    public async Task<object> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = request.ToProduct();
        await productRepository.InsertAsync(product);

        var skus = request.Skus.Select(s => s.ToSku(request, product.Id)).ToList();
        foreach (var sku in skus)
            await skuRepository.InsertAsync(sku);

        return product.Id;
    }
}
