using CommunalService.Domain.Infrastructure.Redis;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CommunalService.Domain.Infrastructure.Snowflake;

using Yitter.IdGenerator;

/// <summary>WorkerId 后台服务：应用启动时申请雪花 WorkerId 并周期续约，退出时释放租约。</summary>
    public class WorkerIdBackgroundService(RedisWorkerIdProvider provider) : IHostedService
{
    /// <summary>WorkerId 提供者（Redis 租约实现）。</summary>
    private readonly RedisWorkerIdProvider _provider = provider;
    /// <summary>启动时获取 WorkerId 并启动续约；失败抛出阻止服务以无 WorkerId 状态启动。</summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var workerId = await _provider.RegisterAsync();
            Console.WriteLine($"WorkerId 注册成功: {workerId}");

            var options = new IdGeneratorOptions
            {
                WorkerId = workerId,
                WorkerIdBitLength = 6,
                SeqBitLength = 10,
                BaseTime = new DateTime(2020, 1, 1),
                Method = 1
            };
            YitIdHelper.SetIdGenerator(options);
        }
        catch (Exception ex)
        {
            Console.WriteLine("WorkerId 注册失败");
            throw; // 可以决定是否阻止应用启动
        }
    }

    /// <summary>停止续约并释放租约。</summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("正在释放 WorkerId...");
        await _provider.UnregisterAsync();
        Console.WriteLine("WorkerId 已释放");
    }
}
