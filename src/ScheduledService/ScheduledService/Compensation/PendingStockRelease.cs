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
    /// <summary>主键（雪花 ID）。</summary>
    public long Id { get; set; }

    [Column(StringLength = 32)]
    /// <summary>订单号（幂等与对账键）。</summary>
    public string OrderNo { get; set; } = string.Empty;

    /// <summary>商品 SKU ID。</summary>
    public long SkuId { get; set; }

    /// <summary>数量。</summary>
    public int Quantity { get; set; }

    /// <summary>已重试次数。</summary>
    public int RetryCount { get; set; }

    /// <summary>创建时间。</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>下次重试时间。</summary>
    public DateTime NextRetryAt { get; set; } = DateTime.Now;

    [Column(StringLength = 500, IsNullable = true)]
    /// <summary>最近一次失败原因。</summary>
    public string? LastError { get; set; }
}
