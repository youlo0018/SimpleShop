using MediatR;
using MerchantPlatformService.Domain.IRepository;

namespace MerchantPlatformService.Application.Features.Merchants.Get;

public sealed class GetMerchantHandler(IMerchantRepository repository) : IRequestHandler<GetMerchantQuery, object>
{
    public async Task<object> Handle(GetMerchantQuery request, CancellationToken cancellationToken)
    {
        return await repository.GetByIdAsync(request.Id);
    }
}
