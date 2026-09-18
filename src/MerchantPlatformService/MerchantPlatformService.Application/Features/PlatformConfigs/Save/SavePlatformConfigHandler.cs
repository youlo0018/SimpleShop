using MediatR;
using MerchantPlatformService.Domain.Entity;
using MerchantPlatformService.Domain.IRepository;

namespace MerchantPlatformService.Application.Features.PlatformConfigs.Save;

/// <summary>保存平台配置（键值对，Upsert 语义）。</summary>
public sealed class SavePlatformConfigHandler(IPlatformConfigRepository repository)
    : IRequestHandler<SavePlatformConfigCommand, object>
{
    /// <summary>处理入口：保存平台配置（键值对，Upsert 语义）。</summary>
    public async Task<object> Handle(SavePlatformConfigCommand request, CancellationToken cancellationToken)
    {
        var existing = (await repository.QueryAsync(config => config.ConfigKey == request.ConfigKey)).FirstOrDefault();
        var config = existing ?? new PlatformConfig { ConfigKey = request.ConfigKey };
        config.ConfigValue = request.ConfigValue;
        config.Description = request.Description;
        config.ConfigType = request.ConfigType;
        config.IsEnabled = request.IsEnabled;

        if (existing is null)
        {
            await repository.InsertAsync(config);
        }
        else
        {
            await repository.UpdateAsync(config, cancellationToken);
        }

        return new { config.Id, config.ConfigKey, config.ConfigValue, config.IsEnabled };
    }
}
