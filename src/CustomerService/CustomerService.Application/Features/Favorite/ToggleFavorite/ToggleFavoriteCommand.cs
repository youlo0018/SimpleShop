using CommunalService.Domain;
using MediatR;

namespace CustomerService.Application.Features.Favorite.ToggleFavorite;

/// <summary>收藏/取消收藏切换（幂等；客户 ID 由登录态注入）。</summary>
/// <param name="ProductId">商品 ID。</param>
public record ToggleFavoriteCommand(long ProductId) : IRequest<ApiResponse>;
