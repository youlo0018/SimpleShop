namespace AuthService.Infrastructure;

using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // 这一行是关键，它会配置所有 OpenIddict 需要的实体映射
        // 包括 Application, Authorization, Scope, Token 等[reference:3]
        builder.UseOpenIddict();
    }
}