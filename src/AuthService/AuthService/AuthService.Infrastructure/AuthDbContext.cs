using AuthService.Domain.Entity;

namespace AuthService.Infrastructure;

using Microsoft.EntityFrameworkCore;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options)
        : base(options)
    {
    }
    // 使用自定义的客户端实体，继承并添加 ClientType
    public DbSet<UserApplication> Applications { get; set; }
    public DbSet<UserAuthorization> Authorizations { get; set; }
    public DbSet<UserScope> Scopes { get; set; }
    public DbSet<UserToken> Tokens { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // 这一行是关键，它会配置所有 OpenIddict 需要的实体映射
        // 包括 Application, Authorization, Scope, Token 等[reference:3]
        builder.UseOpenIddict<UserApplication, UserAuthorization, UserScope, UserToken, Guid>();
    }
}