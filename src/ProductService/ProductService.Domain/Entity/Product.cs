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
    /// <summary>名称。</summary>
    public string Name { get; set; }

    [Column(StringLength = 255), Description("商品主图")]
    /// <summary>主图地址。</summary>
    public string MainImage { get; set; }

    [Column(StringLength = 2000), Description("商品轮播图（JSON 数组）")]
    /// <summary>轮播图地址列表（JSON 数组字符串，详情页图集用；主图作为兜底第一张）。</summary>
    public string Images { get; set; } = "[]";

    [Description("Category Id")]
    /// <summary>Category Id</summary>
    public long CategoryId { get; set; }

    [Description("Brand Id")]
    /// <summary>Brand Id</summary>
    public long BrandId { get; set; }

    [Column(Precision = 18, Scale = 2), Description("商品最小价格")]
    /// <summary>商品最小价格</summary>
    public decimal MinPrice { get; set; }

    [Column(Precision = 18, Scale = 2), Description("商品最大价格")]
    /// <summary>商品最大价格</summary>
    public decimal MaxPrice { get; set; }

    [Column(StringLength = 255), Description("商品描述")]
    /// <summary>描述。</summary>
    public string Description { get; set; }

    [Description("Status: 0 draft, 1 published, 2 offline")]
    /// <summary>Status: 0 draft, 1 published, 2 offline</summary>
    public int Status { get; set; }
    [Description("Review status: 0 pending, 1 approved, 2 rejected")]
    /// <summary>Review status: 0 pending, 1 approved, 2 rejected</summary>
    public int ReviewStatus { get; set; }

    [Description("操作人ID")] public long OperatorId { get; set; }
    [Description("是否允许使用优惠券")] public bool IsVoucherUsageAllowed { get; set; }
    [Description("是否允许使用积分")] public bool IsPointUsageAllowed { get; set; }
}
