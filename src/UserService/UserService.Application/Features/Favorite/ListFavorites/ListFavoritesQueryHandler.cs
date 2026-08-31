using CommunalService.Domain;
using CommunalService.Domain.Enums;
using MediatR;
using UserService.Domain.IRepository;

namespace UserService.Application.Features.Favorite.ListFavorites;

/// <summary>收藏列表：按创建时间倒序；wildcard 可查任意用户。</summary>
public class ListFavoritesQueryHandler(IFavoriteRepository repository, TenantContext tenant)
    : IRequestHandler<ListFavoritesQuery, ApiResponse>
{
    public async Task<ApiResponse> Handle(ListFavoritesQuery request, CancellationToken cancellationToken)
    {
        var scopedUserId = tenant.HasWildcard ? request.UserId : tenant.UserId;
        var favorites = await repository.QueryAsync(item => item.UserId == scopedUserId && !item.IsDeleted);
        return ApiResults.Ok(favorites.OrderByDescending(item => item.CreatedAt).ToList());
    }
}
