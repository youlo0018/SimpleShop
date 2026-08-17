using CommunalService.Domain.Infrastructure.Redis;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CommunalService.Domain.Infrastructure.Snowflake;

using Yitter.IdGenerator;

public class WorkerIdBackgroundService(RedisWorkerIdProvider provider) : IHostedService
{
    private readonly RedisWorkerIdProvider _provider = provider;
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

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("正在释放 WorkerId...");
        await _provider.UnregisterAsync();
        Console.WriteLine("WorkerId 已释放");
    }
}