using System.Text.Json;
using CartService.Domain;
using StackExchange.Redis;

namespace CartService.Infrastructure;

public sealed class RedisCartStore(IConnectionMultiplexer redis) : ICartStore
{
    private static string Key(long userId) => $"cart:user:{userId}";

    public async Task<bool> AddOrUpdateAsync(long userId, CartItem item, CancellationToken cancellationToken = default)
    {
        var database = redis.GetDatabase();
        var value = JsonSerializer.Serialize(item);
        return await database.HashSetAsync(Key(userId), item.SkuId.ToString(), value);
    }

    public async Task<List<CartItem>> GetAsync(long userId, CancellationToken cancellationToken = default)
    {
        var database = redis.GetDatabase();
        var entries = await database.HashGetAllAsync(Key(userId));
        var items = new List<CartItem>();
        foreach (var entry in entries)
        {
            if (entry.Value.IsNull)
            {
                continue;
            }

            var item = JsonSerializer.Deserialize<CartItem>(entry.Value.ToString());
            if (item is not null)
            {
                items.Add(item);
            }
        }

        return items;
    }

    public async Task<bool> RemoveAsync(long userId, long skuId, CancellationToken cancellationToken = default)
    {
        var database = redis.GetDatabase();
        return await database.HashDeleteAsync(Key(userId), skuId.ToString());
    }
}
