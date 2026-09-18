using System.ComponentModel;
using System.Diagnostics.Contracts;
using CommunalService.Domain.Entity;
using FreeSql.DataAnnotations;

namespace ProductService.Domain.Entity;

[Description("商品规格值表")]
[Table(Name = "specification_value")]
public class SpecificationValue: BaseEntity
{
    [Column(StringLength = 40), Description("商品规格值名称")]
    /// <summary>商品规格值名称</summary>
    public string ValueName { get; set; }
    [Description("商品规格ID")]
    /// <summary>商品规格ID</summary>
    public long SpecificationId { get; set; }
    [Description("排序")]
    /// <summary>排序</summary>
    public int Sort { get; set; }
    
    [Description("操作人ID")] public long OperatorId { get; set; }
}
