using System.ComponentModel;
using CommunalService.Domain.Entity;
using FreeSql.DataAnnotations;

namespace UserService.Domain.Entity;

[Table(Name = "user_favorite")]
public sealed class Favorite : BaseEntity
{
    [Description("用户ID")] public long UserId { get; set; }

    [Description("商品ID")] public long ProductId { get; set; }
}
