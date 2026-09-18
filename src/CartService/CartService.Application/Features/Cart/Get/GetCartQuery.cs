using MediatR;

namespace CartService.Application.Features.Cart.Get;

/// <summary>购物车查询（用户 ID 由登录态注入）。</summary>
/// <param name="UserId">用户 ID。</param>
public sealed record GetCartQuery(long UserId) : IRequest<object>;
