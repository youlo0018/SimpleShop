using ProductService.Domain.Entity;
using EntityProduct = ProductService.Domain.Entity.Product;

namespace ProductService.Application.Features.Product.CreateProduct;

public static class CreateProductMapper
{
    public static EntityProduct ToProduct(this CreateProductCommand command)
    {
        return new EntityProduct
        {
            PlatformId = command.PlatformId,
            MerchantId = command.MerchantId,
            Name = command.Name,
            MainImage = command.MainImage,
            CategoryId = command.CategoryId,
            BrandId = command.BrandId,
            Description = command.Description,
            Status = 0,
            ReviewStatus = 0
        };
    }

    public static Sku ToSku(this CreateSkuItem item, CreateProductCommand command, long productId)
    {
        return new Sku
        {
            PlatformId = command.PlatformId,
            MerchantId = command.MerchantId,
            ProductId = productId,
            SkuCode = item.SkuCode,
            Price = item.Price,
            OriginalPrice = item.OriginalPrice,
            Stock = item.Stock,
            Image = item.Image
        };
    }
}
