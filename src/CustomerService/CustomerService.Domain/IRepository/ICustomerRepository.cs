using CommunalService.Domain.Entity;
using CommunalService.Domain.Interfaces;
using CustomerService.Domain.Entity;

namespace CustomerService.Domain.IRepository;

/// <summary>客户聚合仓储（分层基准示例）：复杂查询在 Infrastructure 实现，Application 不直接依赖 IFreeSql。</summary>
public interface ICustomerRepository<T> : IBaseRepository<T> where T : BaseEntity
{
}

/// <summary>客户账号仓储：注册查重与按用户名取号（登录用）。</summary>
public interface ICustomerAccountRepository : IBaseRepository<Customer>
{
    /// <summary>按平台 + 登录名取客户（排除已注销/软删）：不同平台允许同名账号。</summary>
    Task<Customer?> GetByCustomerNameAsync(long platformId, string customerName);

    /// <summary>平台内登录名或手机号是否已被占用（注册查重，excludeId 用于编辑时排除自身）。</summary>
    Task<bool> ExistsAsync(long platformId, string customerName, string phone, long excludeId = 0);
}

/// <summary>客户地址仓储：本人地址列表与默认地址唯一化。</summary>
public interface ICustomerAddressRepository : IBaseRepository<CustomerAddress>
{
    /// <summary>按客户取地址簿：默认地址排前，其次按创建时间倒序。</summary>
    Task<List<CustomerAddress>> ListByCustomerAsync(long customerId);

    /// <summary>默认地址唯一化：清除该客户其他地址的默认标记。</summary>
    Task ClearDefaultAsync(long customerId, long exceptId);
}

/// <summary>客户收藏仓储：按客户 + 商品定位收藏记录。</summary>
public interface ICustomerFavoriteRepository : IBaseRepository<CustomerFavorite>
{
    /// <summary>取客户对某商品的收藏记录（含软删，用于切换恢复）。</summary>
    Task<CustomerFavorite?> GetAsync(long customerId, long productId);
}
