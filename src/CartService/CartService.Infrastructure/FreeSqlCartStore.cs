using CartService.Domain;
using FreeSql;
using Yitter.IdGenerator;

namespace CartService.Infrastructure;

public sealed class FreeSqlCartStore(IFreeSql freeSql) : ICartStore
{
    public async Task<bool> AddOrUpdateAsync(long userId, CartItem item, CancellationToken cancellationToken = default)
    {
        item.UserId = userId;
        var existing = await freeSql.Select<CartItem>()
            .Where(cart => cart.UserId == userId && cart.SkuId == item.SkuId)
            .FirstAsync(cancellationToken);

        if (existing is null)
        {
            item.Id = YitIdHelper.NextId();
            return await freeSql.Insert(item).ExecuteAffrowsAsync(cancellationToken) > 0;
        }

        existing.ProductId = item.ProductId;
        existing.MerchantId = item.MerchantId;
        existing.PlatformId = item.PlatformId;
        existing.ProductName = item.ProductName;
        existing.Price = item.Price;
        existing.Quantity = item.Quantity;
        existing.Checked = item.Checked;
        return await freeSql.Update<CartItem>().SetSource(existing).ExecuteAffrowsAsync(cancellationToken) > 0;
    }

    public async Task<List<CartItem>> GetAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await freeSql.Select<CartItem>()
            .Where(cart => cart.UserId == userId)
            .OrderByDescending(cart => cart.AddedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> RemoveAsync(long userId, long skuId, CancellationToken cancellationToken = default)
    {
        var item = await freeSql.Select<CartItem>()
            .Where(cart => cart.UserId == userId && cart.SkuId == skuId)
            .FirstAsync(cancellationToken);
        if (item is null) return false;

        item.IsDeleted = true;
        item.DeletedAt = DateTime.Now;
        item.UpdatedAt = DateTime.Now;
        return await freeSql.Update<CartItem>().SetSource(item).ExecuteAffrowsAsync(cancellationToken) > 0;
    }
}
