using System.ComponentModel;
using CommunalService.Domain.Entity;
using FreeSql.DataAnnotations;


namespace ProductService.Domain.Entity;

[Description("商品SKU规格值连接表")]
[Table(Name = "sku_specification")]
[Index("inx_SpecificationValueId_SkuId", "SkuId,SpecificationValueId", IsUnique = true)]
public class SkuSpecification: BaseEntity
{
    public long SkuId { get; set; }
    public long SpecificationValueId { get; set; }
}