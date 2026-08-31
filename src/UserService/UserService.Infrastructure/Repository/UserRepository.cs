using CommunalService.Domain.Infrastructure;
using FreeSql;
using UserService.Domain.Entity;
using UserService.Domain.IRepository;

namespace UserService.Infrastructure.Repository;

public class UserRepository(IFreeSql freeSql) : BaseRepository<User>(freeSql), IUserRepository
{
    public Task<User?> GetByUserNameAsync(string userName)
        => freeSql.Select<User>().Where(item => item.UserName == userName && !item.IsDeleted).FirstAsync()!;

    public Task<bool> ExistsAsync(string userName, string phone, long excludeId = 0)
        => freeSql.Select<User>().AnyAsync(item => !item.IsDeleted && item.Id != excludeId
            && (item.UserName == userName || (!string.IsNullOrWhiteSpace(phone) && item.Phone == phone)));

    public async Task<(List<User> Items, long Total)> QueryPagedAsync(string keyword, string role, int page, int pageSize)
    {
        var selection = freeSql.Select<User>()
            .Where(item => !item.IsDeleted)
            .WhereIf(!string.IsNullOrWhiteSpace(keyword), item =>
                item.UserName.Contains(keyword) || item.Phone.Contains(keyword) || item.Email.Contains(keyword))
            .WhereIf(!string.IsNullOrWhiteSpace(role), item => item.Role == role);
        var total = await selection.CountAsync();
        var items = await selection.OrderByDescending(item => item.CreatedAt)
            .Page(Math.Max(page, 1), pageSize).ToListAsync();
        return (items, total);
    }
}

public class AddressRepository(IFreeSql freeSql) : BaseRepository<Address>(freeSql), IAddressRepository
{
    public Task<List<Address>> ListByUserAsync(long userId)
        => freeSql.Select<Address>().Where(item => item.UserId == userId && !item.IsDeleted)
            .OrderByDescending(item => item.IsDefault).OrderByDescending(item => item.CreatedAt).ToListAsync();

    public async Task ClearDefaultAsync(long userId, long exceptId)
        => await freeSql.Update<Address>()
            .Where(item => item.UserId == userId && item.Id != exceptId && !item.IsDeleted)
            .Set(item => item.IsDefault, false).ExecuteAffrowsAsync();
}

public class FavoriteRepository(IFreeSql freeSql) : BaseRepository<Favorite>(freeSql), IFavoriteRepository
{
    public Task<Favorite?> GetAsync(long userId, long productId)
        => freeSql.Select<Favorite>()
            .Where(item => item.UserId == userId && item.ProductId == productId && !item.IsDeleted).FirstAsync();
}
