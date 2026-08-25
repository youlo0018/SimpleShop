using MediatR;
using MerchantPlatformService.Domain.IRepository;

namespace MerchantPlatformService.Application.Features.PlatformConfigs.Get;

public sealed class GetPlatformConfigHandler(IPlatformConfigRepository repository)
    : IRequestHandler<GetPlatformConfigQuery, object>
{
    public async Task<object> Handle(GetPlatformConfigQuery request, CancellationToken cancellationToken)
    {
        return (await repository.QueryAsync(config => config.ConfigKey == request.ConfigKey)).FirstOrDefault();
    }
}
