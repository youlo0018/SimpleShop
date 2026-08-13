using System.ComponentModel;
using CommunalService.Domain.Attributes;
using FreeSql.DataAnnotations;

namespace CommunalService.Domain.Entity;

public class BaseEntity
{
    /// <summary>
    /// ID
    /// </summary>
    [Snowflake]
    [Column(IsPrimary = true, Position = 1), Description("Id")]
    public long Id { get; set; }

    /// <summary>
    /// 是否已删除
    /// </summary>
    [Column(Position = -4), Description("是否删除")]
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// 创建时间
    /// </summary>
    [Column(Position = -3, IsNullable = true), Description("创建时间")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    

    /// <summary>
    /// 更新时间
    /// </summary>
    [Column(Position = -2, IsNullable = true), Description("更新时间")]
    public DateTime? UpdatedAt { get; set; } = null;

    /// <summary>
    /// 删除时间
    /// </summary>
    [Column(Position = -1, IsNullable = true), Description("删除时间")]
    public DateTime? DeletedAt { get; set; } = null;
}