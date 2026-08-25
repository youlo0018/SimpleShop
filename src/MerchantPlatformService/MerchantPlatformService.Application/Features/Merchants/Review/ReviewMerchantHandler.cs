using MediatR;
using MerchantPlatformService.Domain.Enums;
using MerchantPlatformService.Domain.IRepository;

namespace MerchantPlatformService.Application.Features.Merchants.Review;

public sealed class ReviewMerchantHandler(IMerchantRepository repository) : IRequestHandler<ReviewMerchantCommand, object>
{
    public async Task<object> Handle(ReviewMerchantCommand request, CancellationToken cancellationToken)
    {
        var merchant = await repository.GetByIdAsync(request.Id);
        if (merchant is null)
        {
            return new { success = false, message = "商户不存在" };
        }

        if (merchant.State != MerchantStatus.PendingReview)
        {
            return new { success = false, message = "商户当前状态不可审核" };
        }

        merchant.Status = request.Approved ? (int)MerchantStatus.Approved : (int)MerchantStatus.Rejected;
        merchant.ReviewedAt = DateTime.Now;
        merchant.RejectReason = request.Approved ? null : request.Reason ?? "未通过审核";

        await repository.UpdateAsync(merchant, cancellationToken);
        return new { success = true, merchant.Id, merchant.State, merchant.RejectReason };
    }
}
