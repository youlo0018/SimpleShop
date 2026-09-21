using CommunalService.Domain;
using MediatR;

namespace ProductService.Application.Features.Product.CreateProduct;

public record CreateProductCommand : IRequest<ApiResponse>
{
    /// <summary>平台 ID。</summary>
    public long PlatformId { get; set; }
    /// <summary>商户 ID。</summary>
    public long MerchantId { get; set; }
    /// <summary>名称。</summary>
    public string Name { get; set; }
    /// <summary>主图地址。</summary>
    public string MainImage { get; set; }
    /// <summary>轮播图地址列表（最多 6 张，详情页图集）。</summary>
    public List<string> Images { get; set; } = [];
    /// <summary>分类 ID。</summary>
    public long CategoryId { get; set; }
    /// <summary>品牌 ID。</summary>
    public long BrandId { get; set; }
    /// <summary>描述。</summary>
    public string Description { get; set; }
    /// <summary>SKU 列表。</summary>
    public List<CreateSkuItem> Skus { get; set; }
}

public record CreateSkuItem
{
    /// <summary>SKU 编码（商品内唯一）。</summary>
    public string SkuCode { get; set; }
    /// <summary>单价（元）。</summary>
    public decimal Price { get; set; }
    /// <summary>原价（元）。</summary>
    public decimal OriginalPrice { get; set; }
    /// <summary>库存数量。</summary>
    public int Stock { get; set; }
    /// <summary>图片地址。</summary>
    public string Image { get; set; }
    /// <summary>规格名。</summary>
    public string? SpecName { get; set; }
    /// <summary>规格值。</summary>
    public string? SpecValue { get; set; }
}
