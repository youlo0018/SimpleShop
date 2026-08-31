using CommunalService.Domain.Entity;
using CommunalService.Domain.Interfaces;
using UserService.Domain.Entity;

namespace UserService.Domain.IRepository;

/// <summary>用户聚合仓储；复杂查询在 Infrastructure 实现，Application 不直接依赖 IFreeSql。</summary>
public interface IUserRepository : IBaseRepository<User>
{
    Task<User?> GetByUserNameAsync(string userName);

    /// <summary>用户名或手机号是否已被其他用户占用。</summary>
    Task<bool> ExistsAsync(string userName, string phone, long excludeId = 0);

    /// <summary>后台分页查询（含 keyword/role 过滤），返回页数据与总数。</summary>
    Task<(List<User> Items, long Total)> QueryPagedAsync(string keyword, string role, int page, int pageSize);
}

public interface IAddressRepository : IBaseRepository<Address>
{
    Task<List<Address>> ListByUserAsync(long userId);

    /// <summary>默认地址唯一化：清除该用户其他地址的默认标记。</summary>
    Task ClearDefaultAsync(long userId, long exceptId);
}

public interface IFavoriteRepository : IBaseRepository<Favorite>
{
    Task<Favorite?> GetAsync(long userId, long productId);
}
