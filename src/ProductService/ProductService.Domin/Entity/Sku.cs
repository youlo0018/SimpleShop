using System.ComponentModel;
using CommunalService.Domain.Entity;
using FreeSql.DataAnnotations;

namespace ProductService.Domin.Entity;

[Description("商品SKU表")]
[Table(Name = "sku")]
[Index("inx_SkuCode", "SkuCode", IsUnique = true)]
public class Sku : BaseEntity
{
    [Description("商品ID")] public long ProductId { get; set; }

    [Description("SKU编码"), Column(StringLength = 40)]
    public string SkuCode { get; set; }

    [Description("价格"), Column(Precision = 18, Scale = 2)]
    public decimal Price { get; set; }

    [Description("库存")] public int Stock { get; set; }

    [Description("图片"), Column(StringLength = 255)]
    public string Image { get; set; }
}