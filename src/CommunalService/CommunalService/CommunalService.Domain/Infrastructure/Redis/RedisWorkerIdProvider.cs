using StackExchange.Redis;

namespace CommunalService.Domain.Infrastructure.Redis;

public class RedisWorkerIdProvider : IDisposable
{
    private readonly IDatabase _redisDb;
    private readonly string _workerIdKey = "snowflake:worker_id:seq"; // 用于自增的Key
    private readonly string _leaseKeyPrefix = "snowflake:worker_id:lease:"; // 租约Key前缀
    private readonly int _maxWorkerId; // WorkerId 最大值
    private readonly TimeSpan _leaseDuration = TimeSpan.FromSeconds(30); // 租约时长
    private readonly TimeSpan _renewInterval = TimeSpan.FromSeconds(10); // 续约间隔
    private CancellationTokenSource? _renewCts;
    private ushort? _currentWorkerId;

    public RedisWorkerIdProvider(IConnectionMultiplexer redis, int maxWorkerId = 63)
    {
        _redisDb = redis.GetDatabase();
        _maxWorkerId = maxWorkerId;
    }

    /// <summary>
    /// 注册一个 WorkerId，此方法应只调用一次
    /// </summary>
    public async Task<ushort> RegisterAsync()
    {
        if (_currentWorkerId.HasValue)
            return _currentWorkerId.Value;

        // 1. 原子性地获取一个自增序列号
        long seq = await _redisDb.StringIncrementAsync(_workerIdKey);
        var workerId = (ushort)(seq % (_maxWorkerId + 1));

        // 2. 尝试获取该 WorkerId 的租约
        var leaseKey = $"{_leaseKeyPrefix}{workerId}";
        var acquired = await _redisDb.StringSetAsync(leaseKey, Environment.MachineName, _leaseDuration, When.NotExists);

        if (acquired)
        {
            _currentWorkerId = workerId;
            // 3. 启动后台续约任务
            _renewCts = new CancellationTokenSource();
            _ = RenewLeaseAsync(leaseKey, _renewCts.Token);
            return workerId;
        }
        else
        {
            // 如果获取失败，说明该 WorkerId 已被其他实例占用，需要重试。
            // 在实际应用中，这里应实现重试逻辑，例如循环调用 RegisterAsync。
            throw new Exception($"Unable to acquire lease for WorkerId: {workerId}");
        }
    }

    private async Task RenewLeaseAsync(string leaseKey, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(_renewInterval, cancellationToken);
            // 续约租约
            await _redisDb.KeyExpireAsync(leaseKey, _leaseDuration);
        }
    }

    /// <summary>
    /// 释放当前实例持有的 WorkerId 租约
    /// </summary>
    public async Task UnregisterAsync()
    {
        if (!_currentWorkerId.HasValue) return;

        // 1. 停止续约任务
        _renewCts?.Cancel();
        _renewCts?.Dispose();
        _renewCts = null;

        // 2. 删除租约 Key，释放 WorkerId
        var leaseKey = $"{_leaseKeyPrefix}{_currentWorkerId.Value}";
        await _redisDb.KeyDeleteAsync(leaseKey);

        _currentWorkerId = null;
    }

    public void Dispose()
    {
        // 确保程序退出时释放 WorkerId
        UnregisterAsync().GetAwaiter().GetResult();
    }
}