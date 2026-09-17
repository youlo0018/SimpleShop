using CommunalService.Application.Common;
using CommunalService.Domain;
using MarketingService.Application;
using MarketingService.Application.Features.Activities;
using MarketingService.Application.Messaging;
using MarketingService.Domain.Entity;
using MarketingService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Swagger/OpenAPI 描述（仅开发环境挂载 UI）。
builder.Services.AddOpenApi();
// 共享基础设施：AgileConfig、FreeSql、Redis、雪花 ID、Consul 注册、RabbitMQ、gRPC/Kestrel 双端口。
builder.AddBasicServices();
// 仓储 DI。
builder.AddInfrastructure();
// 应用服务 DI（优惠引擎/落账服务）。
builder.AddApplication();
// MediatR + FluentValidation 管道（Validator 在 Application 程序集）。
builder.AddMediatRWithHandlers(typeof(SaveActivityCommand).Assembly, typeof(ValidationBehavior<,>).Assembly);
// 控制器。
builder.Services.AddControllers();

// 支付成功发满赠券、订单取消退券，各用独立队列消费，不影响订单/库存消费者。
builder.Services.AddHostedService<PaymentSucceededMarketingConsumer>();
// 订单取消消费者：回退券占用。
builder.Services.AddHostedService<OrderCancelledMarketingConsumer>();

var app = builder.Build();
app.AddApplication();
// /health、全局异常与 PV 中间件、MagicOnion 服务映射。
await app.AddBaseInfrastructure();
// 每个服务启动时自治迁移自己的表。
await app.MigrateDatabaseAsync(
    typeof(MarketingActivity), typeof(MarketingActivityTarget), typeof(CouponTemplate), typeof(CouponActivity),
    typeof(CouponActivityTarget), typeof(UserCoupon), typeof(MarketingActivityRecord), typeof(MarketingActivityRecordItem),
    typeof(CouponRecord), typeof(CouponRecordItem), typeof(MarketingConfig));

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();
app.Run();
