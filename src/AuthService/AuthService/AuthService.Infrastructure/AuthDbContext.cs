using AuthService.Domain.Entity;

namespace AuthService.Infrastructure;

using Microsoft.EntityFrameworkCore;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options)
        : base(options)
    {
    }
    // 使用自定义 OpenIddict 客户端实体（公开/机密由框架自带 ClientType 决定）
    /// <summary>OpenIddict 客户端注册表。</summary>
    public DbSet<UserApplication> Applications { get; set; }
    /// <summary>OpenIddict 授权记录。</summary>
    public DbSet<UserAuthorization> Authorizations { get; set; }
    /// <summary>OpenIddict 作用域。</summary>
    public DbSet<UserScope> Scopes { get; set; }
    /// <summary>OpenIddict 令牌存储。</summary>
    public DbSet<UserToken> Tokens { get; set; }

    /// <summary>注册 OpenIddict EF 模型（客户端/授权/作用域/令牌）。</summary>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // 这一行是关键，它会配置所有 OpenIddict 需要的实体映射
        // 包括 Application, Authorization, Scope, Token 等[reference:3]
        builder.UseOpenIddict<UserApplication, UserAuthorization, UserScope, UserToken, Guid>();
    }
}
