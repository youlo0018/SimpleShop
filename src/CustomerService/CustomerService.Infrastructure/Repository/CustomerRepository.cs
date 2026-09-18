using CommunalService.Domain.Infrastructure;
using CustomerService.Domain.Entity;
using CustomerService.Domain.IRepository;
using FreeSql;

namespace CustomerService.Infrastructure.Repository;

/// <summary>客户仓储实现（分层基准示例：继承通用 BaseRepository，只写领域查询）。</summary>
public class CustomerRepository(IFreeSql freeSql) : BaseRepository<Customer>(freeSql), ICustomerRepository<Customer>, ICustomerAccountRepository
{
    /// <inheritdoc />
    public Task<Customer?> GetByCustomerNameAsync(long platformId, string customerName)
        => freeSql.Select<Customer>()
            .Where(item => item.PlatformId == platformId && item.CustomerName == customerName && !item.IsDeleted)
            .FirstAsync()!;

    /// <inheritdoc />
    public Task<bool> ExistsAsync(long platformId, string customerName, string phone, long excludeId = 0)
        => freeSql.Select<Customer>().AnyAsync(item => !item.IsDeleted && item.PlatformId == platformId && item.Id != excludeId
            && (item.CustomerName == customerName || (!string.IsNullOrWhiteSpace(phone) && item.Phone == phone)));
}

/// <summary>客户地址仓储实现。</summary>
public class CustomerAddressRepository(IFreeSql freeSql) : BaseRepository<CustomerAddress>(freeSql), ICustomerAddressRepository
{
    /// <inheritdoc />
    public Task<List<CustomerAddress>> ListByCustomerAsync(long customerId)
        => freeSql.Select<CustomerAddress>().Where(item => item.CustomerId == customerId && !item.IsDeleted)
            .OrderByDescending(item => item.IsDefault).OrderByDescending(item => item.CreatedAt).ToListAsync();

    /// <inheritdoc />
    public async Task ClearDefaultAsync(long customerId, long exceptId)
        => await freeSql.Update<CustomerAddress>()
            .Where(item => item.CustomerId == customerId && item.Id != exceptId && !item.IsDeleted)
            .Set(item => item.IsDefault, false).ExecuteAffrowsAsync();
}

/// <summary>客户收藏仓储实现。</summary>
public class CustomerFavoriteRepository(IFreeSql freeSql) : BaseRepository<CustomerFavorite>(freeSql), ICustomerFavoriteRepository
{
    /// <inheritdoc />
    public Task<CustomerFavorite?> GetAsync(long customerId, long productId)
        => freeSql.Select<CustomerFavorite>()
            .Where(item => item.CustomerId == customerId && item.ProductId == productId && !item.IsDeleted).FirstAsync();
}
