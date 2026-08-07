using FreeSql.DataAnnotations;

namespace CommunalService.Domain.Entity;

public class BaseEntity
{
    /// <summary>
    /// ID
    /// </summary>
    [Column(IsIdentity = true, IsPrimary = true)]
    public long Id { get; set; }
    /// <summary>
    /// 是否已删除
    /// </summary>
    public bool IsDeleted { get; set; }
    /// <summary>
    /// 创建事件
    /// </summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    /// <summary>
    /// 删除时间
    /// </summary>
    public DateTime DeletedAt { get; set; }
    
}