namespace ScheduledService.Jobs;

public sealed class PaymentTimeoutJobOptions
{
    public const string SectionName = "Jobs:PaymentTimeout";

    /// <summary>每批处理数量。</summary>
    public int BatchSize { get; set; } = 50;

    /// <summary>扫描间隔秒数。</summary>
    public int IntervalSeconds { get; set; } = 30;

    /// <summary>
    /// 全局扫描锁的租期要略大于执行间隔，避免实例崩溃后锁长时间阻塞下一轮调度。
    /// </summary>
    public int ScanLockExpirySeconds { get; set; } = 45;
}
