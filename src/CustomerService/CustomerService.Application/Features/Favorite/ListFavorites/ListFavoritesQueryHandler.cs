using CommunalService.Domain;
using CommunalService.Domain.Enums;
using CustomerService.Domain.IRepository;
using MediatR;

namespace CustomerService.Application.Features.Favorite.ListFavorites;

/// <summary>收藏列表：按创建时间倒序；只允许查当前登录客户。</summary>
public class ListFavoritesQueryHandler(ICustomerFavoriteRepository repository, TenantContext tenant)
    : IRequestHandler<ListFavoritesQuery, ApiResponse>
{
    /// <summary>按 X-Claim-UserId 查询本人收藏。</summary>
    public async Task<ApiResponse> Handle(ListFavoritesQuery request, CancellationToken cancellationToken)
    {
        if (tenant.UserId <= 0)
            return ApiResults.Fail(BaseApiResponseCode.Unauthorized, "请先登录");
        var favorites = await repository.QueryAsync(item => item.CustomerId == tenant.UserId && !item.IsDeleted);
        return ApiResults.Ok(favorites.OrderByDescending(item => item.CreatedAt).ToList());
    }
}
