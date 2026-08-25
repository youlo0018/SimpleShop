using CommunalService.Application.Common;
using CommunalService.Domain;
using CommunalService.Domain.Infrastructure;
using OrderService.Application;
using OrderService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.AddBasicServices();
builder.AddInfrastructure();
builder.Services.AddControllers();
// 鏀粯缁撴灉涓嶆槸鈥滄敹鍒板氨绠楀畬鈥濓紝杩欓噷璁╄鍗曞父椹荤洃鍚敮浠樻垚鍔熶簨浠讹紝淇濊瘉鍥炶皟涓㈠け涔熻兘鏈€缁堜竴鑷淬€?builder.Services.AddHostedService<OrderService.Api.Messaging.PaymentSucceededConsumer>();
// 定时扫描过期待支付订单，自动关单并释放库存。
builder.Services.AddHostedService<OrderService.Api.BackgroundJobs.PaymentTimeoutCloseJob>();
builder.AddMediatRWithHandlers(
    typeof(OrderService.Application.DependencyInjection).Assembly,
    typeof(ValidationBehavior<,>).Assembly);

var app = builder.Build();

app.AddApplication();
await app.AddBaseInfrastructure();
await app.MigrateDatabaseAsync(typeof(OrderService.Domain.Entity.Order), typeof(OrderService.Domain.Entity.OrderItem), typeof(OrderService.Domain.Entity.Shipment));

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  
    app.MapOpenApi();
    app.UseSwaggerUI(options => {
        options.SwaggerEndpoint("/openapi/v1.json", "My API V1");
    });
}

app.UseHttpsRedirection();




app.MapControllers();
app.Run();







