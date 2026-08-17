using AuthService.Domain.Entity;
using AuthService.Infrastructure;
using AuthService.Infrastructure.OpenIddict;
using CommunalService.Domain;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.AddBasicServices();
builder.Services.AddControllers();
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
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // 指定登录路径，用于未认证时重定向
        options.LoginPath = "/api/account/login"; 
        // 可选：登出路径
        options.LogoutPath = "/api/account/logout";
    });
builder.Services.AddAuthorization();
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
        // 设置 Token 颁发和验证的相关端点[reference:13]
        options.SetAuthorizationEndpointUris("/api/account/authorize")
            .SetEndSessionEndpointUris("/api/account/logout") // 原 SetLogoutEndpointUris
            .SetTokenEndpointUris("/api/account/token");

        // 启用你需要的授权流程 (Flows)[reference:14]
        options.AllowAuthorizationCodeFlow() // 授权码流程，用于有后端的应用
            .AllowClientCredentialsFlow() // 客户端凭证流程，用于服务器间调用
            .AllowRefreshTokenFlow() // 刷新令牌流程，用于延长会话
            .AllowPasswordFlow(); //账号密码流程，用于用户密码验证    

        // 添加用于签名和加密的开发证书[reference:15]
        options.AddDevelopmentEncryptionCertificate()
            .AddDevelopmentSigningCertificate();

        // 集成 ASP.NET Core[reference:16]
        options.UseAspNetCore()
            .EnableAuthorizationEndpointPassthrough()//SetAuthorizationEndpointUris
            .EnableEndSessionEndpointPassthrough() // 原 EnableLogoutEndpointPassthrough
            .EnableTokenEndpointPassthrough();
        options.RegisterScopes("api1", "api2");   // 注册自定义 scope
    })
    
    // 2.3 配置验证端 (Validation) - 注意：这一步可选，仅当授权中心和 API 在同一项目时
    .AddValidation(options =>
    {
        options.UseLocalServer(); // 使用本地授权服务器
        options.UseAspNetCore();
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
app.UseHttpsRedirection();

app.MapControllers();
app.Run();

