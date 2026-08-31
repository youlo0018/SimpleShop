using CommunalService.Domain;
using MediatR;

namespace ProductService.Application.Features.Product.CreateProduct;

public record CreateProductCommand : IRequest<ApiResponse>
{
    public long PlatformId { get; set; }
    public long MerchantId { get; set; }
    public string Name { get; set; }
    public string MainImage { get; set; }
    public long CategoryId { get; set; }
    public long BrandId { get; set; }
    public string Description { get; set; }
    public List<CreateSkuItem> Skus { get; set; }
}

public record CreateSkuItem
{
    public string SkuCode { get; set; }
    public decimal Price { get; set; }
    public decimal OriginalPrice { get; set; }
    public int Stock { get; set; }
    public string Image { get; set; }
    public string? SpecName { get; set; }
    public string? SpecValue { get; set; }
}
