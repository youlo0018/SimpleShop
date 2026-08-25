using StackExchange.Redis;

namespace CommunalService.Domain.Infrastructure.Locks;

public sealed class RedisDistributedLock(IConnectionMultiplexer redis) : IDistributedLock
{
    private const string ReleaseScript = """
        if redis.call('GET', KEYS[1]) == ARGV[1] then
            return redis.call('DEL', KEYS[1])
        end
        return 0
        """;

    public async Task<IDistributedLockHandle?> AcquireAsync(
        string key,
        TimeSpan expiry,
        TimeSpan? waitTimeout = null,
        CancellationToken cancellationToken = default)
    {
        var database = redis.GetDatabase();
        var deadline = DateTime.UtcNow + (waitTimeout ?? TimeSpan.Zero);
        var token = Guid.NewGuid().ToString("N");

        while (true)
        {
            if (await database.StringSetAsync(key, token, expiry, When.NotExists))
            {
                return new RedisLockHandle(database, key, token);
            }

            if (DateTime.UtcNow >= deadline || cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            await Task.Delay(50, cancellationToken);
        }
    }

    private sealed class RedisLockHandle(IDatabase database, string key, string token) : IDistributedLockHandle
    {
        public string Key { get; } = key;

        public string Token { get; } = token;

        public async ValueTask DisposeAsync()
        {
            await database.ScriptEvaluateAsync(ReleaseScript, [Key], [Token]);
        }
    }
}
