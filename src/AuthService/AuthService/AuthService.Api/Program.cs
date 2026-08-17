using AuthService.Infrastructure;
using CommunalService.Domain;
using FreeSql;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.AddBasicServices();

#region 删除freesql注册使用EF

var descriptor = builder.Services.FirstOrDefault(s => s.ServiceType == typeof(IFreeSql));
if (descriptor != null)
{
    builder.Services.Remove(descriptor);
}
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // 根据你的数据库类型配置，这里以 PostgreSQL 为例
    options.UseNpgsql( builder.Configuration["Basic:sqlConnectionString"]);
});
#endregion


// --- 2. 核心配置：添加 OpenIddict ---
builder.Services.AddOpenIddict()

    // 2.1 配置核心服务 (Core)
    .AddCore(options =>
    {
        // 使用 Entity Framework Core 作为存储
        options.UseEntityFrameworkCore()
            .UseDbContext<ApplicationDbContext>();
    })

    // 2.2 配置服务端 (Server)
    .AddServer(options =>
    {
        // 设置 Token 颁发和验证的相关端点[reference:13]
        options.SetAuthorizationEndpointUris("/connect/authorize")
            .SetEndSessionEndpointUris("/connect/logout")  // 原 SetLogoutEndpointUris
            .SetTokenEndpointUris("/connect/token");

        // 启用你需要的授权流程 (Flows)[reference:14]
        options.AllowAuthorizationCodeFlow() // 授权码流程，用于有后端的应用
            .AllowClientCredentialsFlow() // 客户端凭证流程，用于服务器间调用
            .AllowRefreshTokenFlow()// 刷新令牌流程，用于延长会话
            .AllowPasswordFlow();  //账号密码流程，用于用户密码验证    

        // 添加用于签名和加密的开发证书[reference:15]
        options.AddDevelopmentEncryptionCertificate()
            .AddDevelopmentSigningCertificate();

        // 集成 ASP.NET Core[reference:16]
        options.UseAspNetCore()
            .EnableAuthorizationEndpointPassthrough()
            .EnableEndSessionEndpointPassthrough() // 原 EnableLogoutEndpointPassthrough
            .EnableTokenEndpointPassthrough();
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
}
await app.AddBaseInfrastructure();
app.UseHttpsRedirection();



app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}