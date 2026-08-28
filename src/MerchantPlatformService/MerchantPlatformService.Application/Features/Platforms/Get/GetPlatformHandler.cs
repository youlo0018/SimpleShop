using CommunalService.Domain;
using FreeSql;
using MediatR;
using MerchantPlatformService.Domain.Entity;

namespace MerchantPlatformService.Application.Features.Platforms.Get;

public sealed class GetPlatformHandler(IFreeSql freeSql, TenantContext tenant) : IRequestHandler<GetPlatformQuery, object>
{
    public async Task<object> Handle(GetPlatformQuery request, CancellationToken cancellationToken)
    {
        var selection = freeSql.Select<Platform>()
            .Where(platform => platform.Id == request.Id && !platform.IsDeleted);
        if (tenant.IsPlatform) selection = selection.Where(platform => platform.Id == tenant.PlatformId);
        if (tenant.IsMerchant)
        {
            var platformId = await freeSql.Select<Merchant>()
                .Where(merchant => merchant.Id == tenant.MerchantId && !merchant.IsDeleted)
                .FirstAsync(merchant => merchant.PlatformId, cancellationToken);
            selection = selection.Where(platform => platform.Id == platformId);
        }
        return await selection.FirstAsync(cancellationToken);
    }
}
