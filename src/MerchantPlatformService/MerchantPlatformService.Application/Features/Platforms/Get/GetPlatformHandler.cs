using MediatR;
using MerchantPlatformService.Domain.IRepository;

namespace MerchantPlatformService.Application.Features.Platforms.Get;

public sealed class GetPlatformHandler(IPlatformRepository repository) : IRequestHandler<GetPlatformQuery, object>
{
    public async Task<object> Handle(GetPlatformQuery request, CancellationToken cancellationToken)
    {
        return await repository.GetByIdAsync(request.Id);
    }
}
