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
    /// <summary>SKU编码</summary>
    public string SkuCode { get; set; }

    [Column(StringLength = 40, IsNullable = true), Description("规格名")]
    /// <summary>规格名</summary>
    public string SpecName { get; set; } = string.Empty;

    [Column(StringLength = 120, IsNullable = true), Description("规格值")]
    /// <summary>规格值</summary>
    public string SpecValue { get; set; } = string.Empty;

    [Description("价格"), Column(Precision = 18, Scale = 2)]
    /// <summary>单价（元）。</summary>
    public decimal Price { get; set; }

    [Description("Original price"), Column(Precision = 18, Scale = 2)]
    /// <summary>原价（元）。</summary>
    public decimal OriginalPrice { get; set; }

    [Description("库存")] public int Stock { get; set; }

    [Description("图片"), Column(StringLength = 255)]
    /// <summary>图片地址。</summary>
    public string Image { get; set; }

    [Description("Whether the SKU is active")]
    /// <summary>Whether the SKU is active</summary>
    public bool IsActive { get; set; } = true;
}
