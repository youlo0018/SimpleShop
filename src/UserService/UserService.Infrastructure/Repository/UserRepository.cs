using CommunalService.Domain.Infrastructure;
using FreeSql;
using UserService.Domain.Entity;
using UserService.Domain.IRepository;

namespace UserService.Infrastructure.Repository;

/// <summary>后台账号仓储实现（仅平台/商户/运营账号，C 端客户由 CustomerService 管理）。</summary>
public class UserRepository(IFreeSql freeSql) : BaseRepository<User>(freeSql), IUserRepository
{
    /// <inheritdoc />
    public Task<User?> GetByUserNameAsync(string userName)
        => freeSql.Select<User>().Where(item => item.UserName == userName && !item.IsDeleted).FirstAsync()!;

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string userName, string phone, long excludeId = 0)
        => freeSql.Select<User>().AnyAsync(item => !item.IsDeleted && item.Id != excludeId
            && (item.UserName == userName || (!string.IsNullOrWhiteSpace(phone) && item.Phone == phone)));

    /// <inheritdoc />
    public async Task<(List<User> Items, long Total)> QueryPagedAsync(string keyword, int page, int pageSize)
    {
        var selection = freeSql.Select<User>()
            .Where(item => !item.IsDeleted)
            .WhereIf(!string.IsNullOrWhiteSpace(keyword), item =>
                item.UserName.Contains(keyword) || item.Phone.Contains(keyword) || item.Email.Contains(keyword));
        var total = await selection.CountAsync();
        var items = await selection.OrderByDescending(item => item.CreatedAt)
            .Page(Math.Max(page, 1), pageSize).ToListAsync();
        return (items, total);
    }
}
