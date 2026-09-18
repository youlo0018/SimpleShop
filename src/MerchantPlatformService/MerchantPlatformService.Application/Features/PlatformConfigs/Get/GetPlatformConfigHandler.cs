using MediatR;
using MerchantPlatformService.Domain.IRepository;

namespace MerchantPlatformService.Application.Features.PlatformConfigs.Get;

/// <summary>平台配置查询（按配置键）。</summary>
public sealed class GetPlatformConfigHandler(IPlatformConfigRepository repository)
    : IRequestHandler<GetPlatformConfigQuery, object>
{
    /// <summary>处理入口：平台配置查询（按配置键）。</summary>
    public async Task<object> Handle(GetPlatformConfigQuery request, CancellationToken cancellationToken)
    {
        return (await repository.QueryAsync(config => config.ConfigKey == request.ConfigKey)).FirstOrDefault();
    }
}
