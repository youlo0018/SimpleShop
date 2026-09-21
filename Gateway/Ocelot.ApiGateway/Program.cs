using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;
using Microsoft.Extensions.Options;
using System.Text;
using CommunalService.Domain.Logging;
using CommunalService.Domain.Messaging;
using Consul;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using SimpleShop.Gateway;
using SimpleShop.Gateway.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
// 自定义 RBAC 中间件通过共享 Consul 发现查询权限服务；显式注册客户端，避免 Ocelot 配置顺序影响中间件构建。
var consulHost = builder.Configuration["ServiceDiscoveryProvider:Host"] ?? "127.0.0.1";
var consulPort = int.TryParse(builder.Configuration["ServiceDiscoveryProvider:Port"], out var parsedConsulPort) ? parsedConsulPort : 8500;
builder.Services.AddSingleton<IConsulClient>(_ => new ConsulClient(options =>
{
    options.Address = new Uri($"http://{consulHost}:{consulPort}");
    options.WaitTime = TimeSpan.FromSeconds(10);
}));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.AddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();
builder.Services.AddSingleton<LoggingEventPublisher>();
builder.Services.AddMemoryCache();
// 客户令牌会话存 Redis：网关校验 jti 会话有效性并做滑动续期（与 CustomerService 共库）。
var redisConnection = builder.Configuration["Basic:redisConnectionString"]
    ?? "localhost:6379,password=Aa123456..,defaultDatabase=1";
builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(_ =>
    StackExchange.Redis.ConnectionMultiplexer.Connect(redisConnection));
builder.Services.AddSingleton<CommunalService.Domain.Infrastructure.Consul.IServiceDiscovery,
    CommunalService.Domain.Infrastructure.Consul.ConsulServiceDiscovery>();
builder.Services.AddCors();
builder.Services.AddOcelot(builder.Configuration).AddConsul();

// 宿主机注册的实例必须返回 Docker 网关地址；Ocelot 默认会用 Consul 节点名，容器无法解析。
builder.Services.RemoveAll<Ocelot.Provider.Consul.Interfaces.IConsulServiceBuilder>();
builder.Services.AddScoped<Ocelot.Provider.Consul.Interfaces.IConsulServiceBuilder, HostAddressConsulServiceBuilder>();

var app = builder.Build();

// CORS 必须先于 RBAC 处理，否则浏览器 OPTIONS 预检没有 Authorization，会被误判为未登录。
app.UseCors(policy =>
{
    policy.AllowAnyHeader().AllowAnyMethod();
    if (app.Environment.IsDevelopment())
    {
        policy.SetIsOriginAllowed(_ => true);
    }
    else
    {
        policy.WithOrigins("http://127.0.0.1:8081", "http://localhost:8081");
    }
});

// 认证与 RBAC 必须在 Ocelot 转发前执行，并同步给下游租户过滤。
app.UseMiddleware<SimpleShop.Gateway.Middleware.AdminAuthorizationMiddleware>();

// 本地管理端、H5 商城和联调工具可能直接从不同端口访问；开发环境放开 CORS，生产应改为白名单。
app.UseMiddleware<GrayReleaseContextMiddleware>();
app.UseMiddleware<LoggingEventMiddleware>();
app.Use(async (context, next) =>
{
    if (context.Request.Path.Equals("/health", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status200OK;
        await context.Response.WriteAsJsonAsync(new { status = "Healthy" });
        return;
    }

    await next();
});
await app.UseOcelot();
app.Run();

public sealed class HostAddressConsulServiceBuilder(IHttpContextAccessor httpContextAccessor)
    : Ocelot.Provider.Consul.DefaultConsulServiceBuilder(httpContextAccessor, null!, null!)
{
    protected override string GetDownstreamHost(
        Consul.ServiceEntry entry,
        Node node) => entry.Service.Address;
}
