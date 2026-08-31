using CommunalService.Domain;
using MediatR;

namespace UserService.Application.Features.Favorite.ListFavorites;

public record ListFavoritesQuery(long UserId = 0) : IRequest<ApiResponse>;
