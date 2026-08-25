using MediatR;
using MerchantPlatformService.Domain.Entity;
using MerchantPlatformService.Domain.Enums;
using MerchantPlatformService.Domain.IRepository;

namespace MerchantPlatformService.Application.Features.Merchants.Create;

public sealed class CreateMerchantHandler(
    IMerchantRepository merchantRepository,
    IPlatformRepository platformRepository) : IRequestHandler<CreateMerchantCommand, object>
{
    public async Task<object> Handle(CreateMerchantCommand request, CancellationToken cancellationToken)
    {
        var platform = await platformRepository.GetByIdAsync(request.PlatformId);
        if (platform is null || !platform.IsEnabled)
        {
            return new { success = false, message = "平台不存在或未启用，不能入驻" };
        }

        var merchant = new Merchant
        {
            PlatformId = request.PlatformId,
            MerchantNo = $"M{DateTimeOffset.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}",
            MerchantName = request.MerchantName,
            ContactName = request.ContactName,
            ContactPhone = request.ContactPhone,
            ContactEmail = request.ContactEmail,
            CommissionRate = request.CommissionRate,
            Status = (int)MerchantStatus.PendingReview
        };

        await merchantRepository.InsertAsync(merchant);
        return new { merchant.Id, merchant.MerchantNo, merchant.State };
    }
}
