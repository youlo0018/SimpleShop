using CommunalService.Application.Common;
using CommunalService.Domain;
using MarketingService.Application;
using MarketingService.Application.Features.Activities;
using MarketingService.Application.Messaging;
using MarketingService.Domain.Entity;
using MarketingService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.AddBasicServices();
builder.AddInfrastructure();
builder.AddApplication();
builder.AddMediatRWithHandlers(typeof(SaveActivityCommand).Assembly, typeof(ValidationBehavior<,>).Assembly);
builder.Services.AddControllers();

// 支付成功发满赠券、订单取消退券，各用独立队列消费，不影响订单/库存消费者。
builder.Services.AddHostedService<PaymentSucceededMarketingConsumer>();
builder.Services.AddHostedService<OrderCancelledMarketingConsumer>();

var app = builder.Build();
app.AddApplication();
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
