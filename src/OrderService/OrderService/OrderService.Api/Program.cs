using CommunalService.Application.Common;
using CommunalService.Domain;
using OrderService.Application;
using OrderService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.AddBasicServices();
builder.AddInfrastructure();
builder.Services.AddControllers();

// 支付结果不是“收到就算完成”；订单常驻监听支付成功事件，保证回调丢失也能最终一致。
// 支付超时关单已迁移到 ScheduledService，避免 API 扩缩容影响调度语义。
builder.Services.AddHostedService<OrderService.Api.Messaging.PaymentSucceededConsumer>();
builder.Services.AddHostedService<OrderService.Api.Messaging.PaymentRefundedConsumer>();
builder.AddMediatRWithHandlers(
    typeof(OrderService.Application.DependencyInjection).Assembly,
    typeof(ValidationBehavior<,>).Assembly);

var app = builder.Build();

app.AddApplication();
await app.AddBaseInfrastructure();
await app.MigrateDatabaseAsync(
    typeof(OrderService.Domain.Entity.Order),
    typeof(OrderService.Domain.Entity.OrderItem),
    typeof(OrderService.Domain.Entity.Shipment),
    typeof(OrderService.Domain.Entity.ShipmentItem),
    typeof(OrderService.Domain.Entity.PendingStockRelease));

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "My API V1"));
}

if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();
app.MapControllers();
app.Run();
