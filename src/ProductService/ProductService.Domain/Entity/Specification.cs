using System.ComponentModel;
using CommunalService.Domain.Entity;
using FreeSql.DataAnnotations;

namespace ProductService.Domain.Entity;

[Description("商品规格表")]
[Table(Name = "specification")]
public sealed class Specification: BaseEntity
{
    [Column(StringLength = 40), Description("商品规格名称")]
    /// <summary>商品规格名称</summary>
    public string SpecName { get; set; }

    [Description("店铺ID")] public long ShopId { get; set; }
    [Description("排序")] public int Sort { get; set; }
    [Description("是否允许自定义规格值")]
    /// <summary>店铺ID</summary>
    public bool IsCustomizationAllowed { get; set; }

    [Description("操作人ID")] public long OperatorId { get; set; }
}
