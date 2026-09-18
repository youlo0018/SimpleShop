using System.ComponentModel;
using CommunalService.Domain.Entity;
using FreeSql.DataAnnotations;

namespace ProductService.Domain.Entity;

[Table(Name = "product_brand")]
public class Brand : BaseEntity
{
    [Column(StringLength = 64), Description("品牌名称")]
    /// <summary>名称。</summary>
    public string Name { get; set; }

    [Column(StringLength = 256, IsNullable = true), Description("品牌Logo URL")]
    /// <summary>品牌Logo URL</summary>
    public string LogoUrl { get; set; }

    [Description("是否启用")]
    /// <summary>是否启用</summary>
    public bool IsActive { get; set; } = true;
}
