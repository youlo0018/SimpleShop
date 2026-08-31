namespace CommunalService.Domain.Infrastructure.Locks;

/// <summary>
/// 分布式锁抽象：跨进程互斥的唯一手段（多实例部署下的定时任务、支付回调、库存扣减都依赖它）。
/// expiry 是锁的自动过期时间（持有者崩溃后的自愈上限）；waitTimeout 是获取锁的等待时长，返回 null 表示获取失败。
/// </summary>
public interface IDistributedLock
{
    Task<IDistributedLockHandle?> AcquireAsync(
        string key,
        TimeSpan expiry,
        TimeSpan? waitTimeout = null,
        CancellationToken cancellationToken = default);
}

public interface IDistributedLockHandle : IAsyncDisposable
{
    string Key { get; }

    string Token { get; }
}
