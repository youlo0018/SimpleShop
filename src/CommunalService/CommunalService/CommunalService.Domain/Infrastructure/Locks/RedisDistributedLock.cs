using StackExchange.Redis;

namespace CommunalService.Domain.Infrastructure.Locks;

/// <summary>
/// Redis 分布式锁实现（SET NX + 释放脚本校验）。
/// 加锁：SET key token NX EX——只有一个实例能写入成功；未获取到则每 50ms 轮询重试，超过等待时限返回 null。
/// 释放：Dispose 时执行 Lua 脚本，只有“锁里的 token 等于自己的 token”才 DEL——防止把别的实例因超时新获得的锁误删。
/// 使用约定：必须 await using 包住临界区（锁随 Dispose 释放）；key 全局统一规划（见业务文档附录锁键表）。
/// </summary>
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
