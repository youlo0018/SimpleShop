using System.ComponentModel;
using CommunalService.Domain.Entity;
using FreeSql.DataAnnotations;

namespace ProductService.Domain.Entity;

[Table(Name = "product_brand")]
public class Brand : BaseEntity
{
    [Column(StringLength = 64), Description("品牌名称")]
    public string Name { get; set; }

    [Column(StringLength = 256, IsNullable = true), Description("品牌Logo URL")]
    public string LogoUrl { get; set; }

    [Description("是否启用")]
    public bool IsActive { get; set; } = true;
}
