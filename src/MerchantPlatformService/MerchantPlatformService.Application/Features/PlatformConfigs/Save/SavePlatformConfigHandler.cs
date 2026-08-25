using MediatR;
using MerchantPlatformService.Domain.Entity;
using MerchantPlatformService.Domain.IRepository;

namespace MerchantPlatformService.Application.Features.PlatformConfigs.Save;

public sealed class SavePlatformConfigHandler(IPlatformConfigRepository repository)
    : IRequestHandler<SavePlatformConfigCommand, object>
{
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
