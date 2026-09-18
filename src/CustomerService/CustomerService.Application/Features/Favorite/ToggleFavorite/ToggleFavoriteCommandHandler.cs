using CommunalService.Domain;
using CommunalService.Domain.Enums;
using CustomerService.Domain.Entity;
using CustomerService.Domain.IRepository;
using MediatR;

namespace CustomerService.Application.Features.Favorite.ToggleFavorite;

/// <summary>收藏/取消收藏（幂等切换）：已收藏则软删，未收藏则插入。</summary>
public class ToggleFavoriteCommandHandler(ICustomerFavoriteRepository repository, TenantContext tenant)
    : IRequestHandler<ToggleFavoriteCommand, ApiResponse>
{
    /// <summary>切换本人收藏状态。</summary>
    public async Task<ApiResponse> Handle(ToggleFavoriteCommand request, CancellationToken cancellationToken)
    {
        var customerId = tenant.UserId;
        if (customerId <= 0)
            return ApiResults.Fail(BaseApiResponseCode.Unauthorized, "请先登录");

        var favorite = await repository.GetAsync(customerId, request.ProductId);
        if (favorite is null)
        {
            await repository.InsertAsync(new CustomerFavorite { CustomerId = customerId, ProductId = request.ProductId });
            return ApiResults.Ok(new { favorited = true });
        }

        favorite.IsDeleted = true;
        favorite.DeletedAt = DateTime.Now;
        await repository.UpdateAsync(favorite);
        return ApiResults.Ok(new { favorited = false });
    }
}
