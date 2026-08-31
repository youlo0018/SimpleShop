using CommunalService.Domain;
using CommunalService.Domain.Enums;
using MediatR;
using FavoriteEntity = UserService.Domain.Entity.Favorite;
using UserService.Domain.IRepository;

namespace UserService.Application.Features.Favorite.ToggleFavorite;

/// <summary>收藏/取消收藏（幂等切换）：已收藏则软删，未收藏则插入。</summary>
public class ToggleFavoriteCommandHandler(IFavoriteRepository repository, TenantContext tenant)
    : IRequestHandler<ToggleFavoriteCommand, ApiResponse>
{
    public async Task<ApiResponse> Handle(ToggleFavoriteCommand request, CancellationToken cancellationToken)
    {
        if (!tenant.HasWildcard && !tenant.IsCustomer)
            return ApiResults.Fail(BaseApiResponseCode.Forbidden, "无权管理收藏");

        var scopedUserId = tenant.HasWildcard ? request.UserId : tenant.UserId;
        var favorite = await repository.GetAsync(scopedUserId, request.ProductId);
        if (favorite is null)
        {
            await repository.InsertAsync(new FavoriteEntity { UserId = scopedUserId, ProductId = request.ProductId });
            return ApiResults.Ok(new { favorited = true });
        }

        favorite.IsDeleted = true;
        favorite.DeletedAt = DateTime.Now;
        await repository.UpdateAsync(favorite);
        return ApiResults.Ok(new { favorited = false });
    }
}
