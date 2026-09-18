using CustomerService.Domain.Entity;
using StackExchange.Redis;

namespace CustomerService.Application.Services;

/// <summary>
/// 客户令牌会话（Redis 服务端存储，不使用本地缓存）：
/// 1) 登录/注册签发令牌时写入 `auth:customer:token:{jti}`（TTL 12h），网关据此校验会话是否有效；
/// 2) 网关在剩余有效期低于阈值时滑动续期（活跃用户不掉线）；
/// 3) 刷新接口换发新 jti 并删除旧键；登出直接删除会话键，实现服务端即时失效。
/// </summary>
public sealed class CustomerTokenService(IDatabase database, CustomerTokenIssuer issuer)
{
    /// <summary>会话有效期（与令牌 exp 一致）。</summary>
    public static readonly TimeSpan Ttl = TimeSpan.FromHours(12);

    /// <summary>签发令牌并登记 Redis 会话。</summary>
    /// <param name="customer">客户账号（平台/Id 会写入令牌声明）。</param>
    /// <returns>客户 JWT。</returns>
    public async Task<string> IssueAsync(Customer customer)
    {
        var jti = Guid.NewGuid().ToString("N");
        await database.StringSetAsync(Key(jti), customer.Id, Ttl);
        return issuer.CreateToken(customer, jti);
    }

    /// <summary>刷新令牌：旧 jti 必须仍在会话中；换发新令牌并删除旧会话键。</summary>
    /// <param name="jti">当前令牌 jti（网关注入的 X-Claim-Jti）。</param>
    /// <param name="customer">已登录客户。</param>
    /// <returns>新令牌；旧会话不存在时返回 null（需重新登录）。</returns>
    public async Task<string?> RefreshAsync(string jti, Customer customer)
    {
        if (string.IsNullOrWhiteSpace(jti) || !await database.KeyExistsAsync(Key(jti))) return null;
        await database.KeyDeleteAsync(Key(jti));
        return await IssueAsync(customer);
    }

    /// <summary>吊销会话（登出）：删除 Redis 键后该令牌立即失效。</summary>
    /// <param name="jti">当前令牌 jti。</param>
    public async Task RevokeAsync(string jti)
    {
        if (!string.IsNullOrWhiteSpace(jti)) await database.KeyDeleteAsync(Key(jti));
    }

    /// <summary>会话键：按 jti 索引，值存客户 Id。</summary>
    private static string Key(string jti) => $"auth:customer:token:{jti}";
}
