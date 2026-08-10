using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CommunalService.Domain;

using Yitter.IdGenerator;

public class WorkerIdBackgroundService : IHostedService
{
    private readonly RedisWorkerIdProvider _provider;
    private readonly ILogger<WorkerIdBackgroundService> _logger;

    public WorkerIdBackgroundService(
        RedisWorkerIdProvider provider,
        ILogger<WorkerIdBackgroundService> logger)
    {
        _provider = provider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var workerId = await _provider.RegisterAsync();
            _logger.LogInformation("WorkerId 注册成功: {WorkerId}", workerId);

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
            _logger.LogError(ex, "WorkerId 注册失败");
            throw; // 可以决定是否阻止应用启动
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("正在释放 WorkerId...");
        await _provider.UnregisterAsync();
        _logger.LogInformation("WorkerId 已释放");
    }
}