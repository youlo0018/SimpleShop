using CommunalService.Domain;
using MediatR;

namespace UserService.Application.Features.Favorite.ToggleFavorite;

public record ToggleFavoriteCommand(long UserId, long ProductId) : IRequest<ApiResponse>;
