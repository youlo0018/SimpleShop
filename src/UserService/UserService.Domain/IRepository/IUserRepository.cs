using CommunalService.Domain.Entity;
using CommunalService.Domain.Interfaces;
using UserService.Domain.Entity;

namespace UserService.Domain.IRepository;

/// <summary>
/// 后台账号聚合仓储（UserService 只服务后台账号；C 端客户见 CustomerService）：
/// 复杂查询在 Infrastructure 实现，Application 不直接依赖 IFreeSql。
/// </summary>
public interface IUserRepository : IBaseRepository<User>
{
    /// <summary>按登录名取后台账号（AuthService 密码校验用）。</summary>
    Task<User?> GetByUserNameAsync(string userName);

    /// <summary>用户名或手机号是否已被其他账号占用。</summary>
    Task<bool> ExistsAsync(string userName, string phone, long excludeId = 0);

    /// <summary>后台分页查询（含 keyword/role 过滤），返回页数据与总数。</summary>
    Task<(List<User> Items, long Total)> QueryPagedAsync(string keyword, int page, int pageSize);
}
