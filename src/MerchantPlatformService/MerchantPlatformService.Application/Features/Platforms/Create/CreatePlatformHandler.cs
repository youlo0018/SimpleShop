using MediatR;
using MerchantPlatformService.Domain.Entity;
using MerchantPlatformService.Domain.IRepository;

namespace MerchantPlatformService.Application.Features.Platforms.Create;

public sealed class CreatePlatformHandler(IPlatformRepository repository) : IRequestHandler<CreatePlatformCommand, object>
{
    public async Task<object> Handle(CreatePlatformCommand request, CancellationToken cancellationToken)
    {
        var exists = await repository.QueryAsync(platform => platform.PlatformCode == request.PlatformCode);
        if (exists.Count > 0)
        {
            return new { success = false, message = "平台编码已存在" };
        }

        var platform = new Platform
        {
            PlatformCode = request.PlatformCode,
            PlatformName = request.PlatformName,
            ContactEmail = request.ContactEmail,
            DefaultCommissionRate = request.DefaultCommissionRate,
            IsEnabled = true
        };

        await repository.InsertAsync(platform);
        return new { platform.Id, platform.PlatformCode, platform.IsEnabled };
    }
}
