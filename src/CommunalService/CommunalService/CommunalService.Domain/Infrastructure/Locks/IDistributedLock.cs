namespace CommunalService.Domain.Infrastructure.Locks;

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
