using System.Text.Json;
using ProductService.Domain.Entity;
using EntityProduct = ProductService.Domain.Entity.Product;

namespace ProductService.Application.Features.Product.CreateProduct;

public static class CreateProductMapper
{
    /// <summary>命令 → 商品实体映射（字段默认值在此收敛）。</summary>
    public static EntityProduct ToProduct(this CreateProductCommand command)
    {
        return new EntityProduct
        {
            PlatformId = command.PlatformId,
            MerchantId = command.MerchantId,
            Name = command.Name,
            MainImage = command.MainImage,
            Images = JsonSerializer.Serialize(command.Images ?? []),
            CategoryId = command.CategoryId,
            BrandId = command.BrandId,
            Description = command.Description,
            Status = 0,
            ReviewStatus = 0
        };
    }

    /// <summary>SKU 项 → SKU 实体映射（库存/图片等默认值在此收敛）。</summary>
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
            Image = item.Image,
            SpecName = item.SpecName ?? string.Empty,
            SpecValue = item.SpecValue ?? string.Empty
        };
    }
}
