using System.ComponentModel;
using FreeSql.DataAnnotations;
using CommunalService.Domain.Entity;

namespace ProductService.Domain.Entity;

[Table(Name = "product")]
public sealed class Product : BaseEntity
{
    [Description("Platform Id")] public long PlatformId { get; set; }

    [Description("Merchant Id")] public long MerchantId { get; set; }

    [Column(StringLength = 40), Description("商品名称")]
    public string Name { get; set; }

    [Column(StringLength = 255), Description("商品主图")]
    public string MainImage { get; set; }

    [Description("Category Id")]
    public long CategoryId { get; set; }

    [Description("Brand Id")]
    public long BrandId { get; set; }

    [Column(Precision = 18, Scale = 2), Description("商品最小价格")]
    public decimal MinPrice { get; set; }

    [Column(Precision = 18, Scale = 2), Description("商品最大价格")]
    public decimal MaxPrice { get; set; }

    [Column(StringLength = 255), Description("商品描述")]
    public string Description { get; set; }

    [Description("Status: 0 draft, 1 published, 2 offline")]
    public int Status { get; set; }
    [Description("Review status: 0 pending, 1 approved, 2 rejected")]
    public int ReviewStatus { get; set; }

    [Description("操作人ID")] public long OperatorId { get; set; }
    [Description("是否允许使用优惠券")] public bool IsVoucherUsageAllowed { get; set; }
    [Description("是否允许使用积分")] public bool IsPointUsageAllowed { get; set; }
}
