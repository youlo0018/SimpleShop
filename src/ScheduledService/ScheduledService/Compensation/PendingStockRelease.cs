using FreeSql.DataAnnotations;

namespace ScheduledService.Compensation;

/// <summary>
/// 关单成功但释放库存失败的待补偿记录。它把跨服务失败从日志变成可重试状态。
/// </summary>
[Table(Name = "pending_stock_release")]
[Index("uk_pending_release", nameof(OrderNo) + "," + nameof(SkuId), true)]
public sealed class PendingStockRelease
{
    [Column(IsPrimary = true)]
    public long Id { get; set; }

    [Column(StringLength = 32)]
    public string OrderNo { get; set; } = string.Empty;

    public long SkuId { get; set; }

    public int Quantity { get; set; }

    public int RetryCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime NextRetryAt { get; set; } = DateTime.Now;

    [Column(StringLength = 500, IsNullable = true)]
    public string? LastError { get; set; }
}
