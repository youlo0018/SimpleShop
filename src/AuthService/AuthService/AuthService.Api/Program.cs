using AuthService.Domain.Entity;
using AuthService.Infrastructure;
using AuthService.Infrastructure.OpenIddict;
using CommunalService.Domain;
using CommunalService.Application.Common;
using AuthService.Application.Features.User.Login;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.AddBasicServices();
builder.Services.AddControllers();
builder.AddMediatRWithHandlers(
    typeof(LoginCommand).Assembly,
    typeof(ValidationBehavior<,>).Assembly);
#region 删除freesql注册使用EF

var descriptor = builder.Services.FirstOrDefault(s => s.ServiceType == typeof(IFreeSql));
if (descriptor != null)
{
    builder.Services.Remove(descriptor);
}

builder.Services.AddDbContext<AuthDbContext>(options =>
{
    // 根据你的数据库类型配置，这里以 PostgreSQL 为例
    var connectionString = builder.Configuration["Basic:sqlConnectionString"];
    Console.WriteLine($"Connection string: {connectionString}");
    options.UseNpgsql(connectionString);

});

#endregion
// 仅需认证服务承载 OpenIddict 方案；后台登录只走密码流令牌端点，不再使用 Cookie 会话。
builder.Services.AddAuthentication();
// --- 2. 核心配置：添加 OpenIddict ---
builder.Services.AddOpenIddict()

    // 2.1 配置核心服务 (Core)
    .AddCore(options =>
    {
        // 使用 Entity Framework Core 作为存储
        options.UseEntityFrameworkCore()
            .UseDbContext<AuthDbContext>()
            .ReplaceDefaultEntities<UserApplication, UserAuthorization, UserScope, UserToken, Guid>();
        ;
    })

    // 2.2 配置服务端 (Server)
    .AddServer(options =>
    {
        // 令牌端点走网关可达路径（/gateway/auth/Token → /api/Auth/Token），由 AuthController 处理 password flow。
        options.SetTokenEndpointUris("/api/Auth/Token");
        options.SetIssuer(new Uri("https://simpleshop.local/auth"));
        options.RegisterAudiences("SimpleShop");

        // 后台登录只启用密码流（后台账号在 UserService，客户令牌由 CustomerService 签发）。
        options.AllowPasswordFlow();
        // 后台是浏览器 SPA（公开客户端，无 client_secret）：允许匿名客户端使用密码流。
        options.AcceptAnonymousClients();

        // 非对称签名（RS256）：证书由 AuthService 生成到共享路径，网关读取同一证书验签；
        // 访问令牌不加密（DisableAccessTokenEncryption），网关直接校验 JWT 签名。
        var signingCertificate = CommunalService.Domain.Infrastructure.LocalSigningCertificate.LoadOrCreate(builder.Configuration);
        options.AddSigningCertificate(signingCertificate);
        options.AddEncryptionKey(new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Auth:TokenSecret"] ?? "SimpleShop.Dev.Token.Secret.2026")));
        options.DisableAccessTokenEncryption();
        options.SetAccessTokenLifetime(TimeSpan.FromHours(12));

        // 自定义声明必须注册，否则不会写入访问令牌。
        options.RegisterClaims("permission", "tenant_type", "platform_id", "merchant_id");

        // 令牌端点交给控制器处理（password flow 需要自定义账号校验）。
        // 开发环境经网关走 HTTP：关闭 OpenIddict 的 HTTPS 强制（生产应由网关/负载均衡终结 TLS）。
        options.UseAspNetCore().EnableTokenEndpointPassthrough().DisableTransportSecurityRequirement();
        options.RegisterScopes("api1", "api2");   // 注册自定义 scope
    });



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => { options.SwaggerEndpoint("/openapi/v1.json", "My API V1"); });
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await OpenIddictOperation.SeedClientsAsync(dbContext);
}
app.UseAuthentication();
app.UseAuthorization();

await app.AddBaseInfrastructure();

app.MapControllers();
app.Run();
