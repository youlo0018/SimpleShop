using MediatR;
using MerchantPlatformService.Domain.IRepository;

namespace MerchantPlatformService.Application.Features.Merchants.Get;

/// <summary>商户详情查询（按租户裁剪）。</summary>
public sealed class GetMerchantHandler(IMerchantRepository repository) : IRequestHandler<GetMerchantQuery, object>
{
    /// <summary>处理入口：商户详情查询（按租户裁剪）。</summary>
    public async Task<object> Handle(GetMerchantQuery request, CancellationToken cancellationToken)
    {
        return await repository.GetByIdAsync(request.Id);
    }
}
