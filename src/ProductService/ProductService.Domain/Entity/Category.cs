using System.ComponentModel;
using CommunalService.Domain.Entity;
using FreeSql.DataAnnotations;

namespace ProductService.Domain.Entity;

[Table(Name = "product_category")]
public class Category : BaseEntity
{
    [Column(StringLength = 64), Description("分类名称")]
    public string Name { get; set; }

    [Description("父级ID，0为顶级")]
    public long ParentId { get; set; }

    [Description("排序权重，越小越靠前")]
    public int Sort { get; set; }

    [Description("是否启用")]
    public bool IsActive { get; set; } = true;
}
