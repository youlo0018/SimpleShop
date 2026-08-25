using System.ComponentModel;
using CommunalService.Domain.Entity;
using FreeSql.DataAnnotations;

namespace ProductService.Domain.Entity;

[Description("商品SKU表")]
[Table(Name = "sku")]
[Index("inx_SkuCode", "SkuCode", IsUnique = true)]
public class Sku : BaseEntity
{
    [Description("Platform Id")] public long PlatformId { get; set; }

    [Description("Merchant Id")] public long MerchantId { get; set; }

    [Description("商品ID")] public long ProductId { get; set; }

    [Description("SKU编码"), Column(StringLength = 40)]
    public string SkuCode { get; set; }

    [Description("价格"), Column(Precision = 18, Scale = 2)]
    public decimal Price { get; set; }

    [Description("Original price"), Column(Precision = 18, Scale = 2)]
    public decimal OriginalPrice { get; set; }

    [Description("库存")] public int Stock { get; set; }

    [Description("图片"), Column(StringLength = 255)]
    public string Image { get; set; }

    [Description("Whether the SKU is active")]
    public bool IsActive { get; set; } = true;
}
