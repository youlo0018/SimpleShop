using CartService.Domain;

namespace CartService.Domain;

public interface ICartStore
{
    Task<bool> AddOrUpdateAsync(long userId, CartItem item, CancellationToken cancellationToken = default);
    Task<List<CartItem>> GetAsync(long userId, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(long userId, long skuId, CancellationToken cancellationToken = default);
}
