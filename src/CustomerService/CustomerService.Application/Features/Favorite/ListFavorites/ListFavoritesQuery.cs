using CommunalService.Domain;
using MediatR;

namespace CustomerService.Application.Features.Favorite.ListFavorites;

/// <summary>客户收藏列表查询（只允许本人）。</summary>
public record ListFavoritesQuery : IRequest<ApiResponse>;
