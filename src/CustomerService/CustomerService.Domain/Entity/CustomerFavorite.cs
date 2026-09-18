using System.ComponentModel;
using CommunalService.Domain.Entity;
using FreeSql.DataAnnotations;

namespace CustomerService.Domain.Entity;

/// <summary>客户商品收藏（软删切换收藏状态）。</summary>
[Table(Name = "customer_favorite")]
[Index("uk_customer_favorite", nameof(CustomerId) + "," + nameof(ProductId), true)]
public sealed class CustomerFavorite : BaseEntity
{
    [Description("客户ID")] public long CustomerId { get; set; }

    [Description("商品ID")] public long ProductId { get; set; }
}
