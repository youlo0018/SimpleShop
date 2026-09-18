using System.ComponentModel;
using FreeSql.DataAnnotations;

namespace OrderService.Domain.Entity;

/// <summary>
/// 关单/取消后释放库存失败的待补偿记录（与 ScheduledService 共用 pending_stock_release 表）。
/// 它把跨服务失败从日志变成可重试状态，由定时补偿任务扫描重试。
/// </summary>
[Table(Name = "pending_stock_release")]
[Index("uk_pending_release", nameof(OrderNo) + "," + nameof(SkuId), true)]
public sealed class PendingStockRelease
{
    /// <summary>主键（雪花）。</summary>
    [Column(IsPrimary = true)]
    public long Id { get; set; }

    /// <summary>订单号（幂等键之一）。</summary>
    [Column(StringLength = 32)]
    public string OrderNo { get; set; } = string.Empty;

    /// <summary>待释放的 SKU。</summary>
    public long SkuId { get; set; }

    /// <summary>待释放数量。</summary>
    public int Quantity { get; set; }

    /// <summary>已重试次数。</summary>
    public int RetryCount { get; set; }

    /// <summary>创建时间。</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>下次重试时间。</summary>
    public DateTime NextRetryAt { get; set; } = DateTime.Now;

    /// <summary>最近一次失败原因。</summary>
    [Column(StringLength = 500, IsNullable = true)]
    public string? LastError { get; set; }
}
