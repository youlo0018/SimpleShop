using System.ComponentModel;
using CommunalService.Domain.Entity;
using FreeSql.DataAnnotations;

namespace ProductService.Domain.Entity;

[Table(Name = "product_category")]
public class Category : BaseEntity
{
    [Column(StringLength = 64), Description("分类名称")]
    /// <summary>名称。</summary>
    public string Name { get; set; }

    [Description("父级ID，0为顶级")]
    /// <summary>父级ID，0为顶级</summary>
    public long ParentId { get; set; }

    [Description("排序权重，越小越靠前")]
    /// <summary>排序权重，越小越靠前</summary>
    public int Sort { get; set; }

    [Description("是否启用")]
    /// <summary>是否启用</summary>
    public bool IsActive { get; set; } = true;
}
