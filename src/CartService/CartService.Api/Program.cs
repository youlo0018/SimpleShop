using CartService.Application;
using CartService.Domain;
using CartService.Application.Features.Cart.Add;
using CommunalService.Application.Common;
using CommunalService.Domain;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.AddBasicServices();
builder.AddApplication();
builder.AddMediatRWithHandlers(typeof(AddCartItemCommand).Assembly, typeof(ValidationBehavior<,>).Assembly);
builder.Services.AddControllers();

var app = builder.Build();
app.AddApplication();
await app.AddBaseInfrastructure();
// 每个服务启动时自治迁移自己的表，避免跨库初始化造成部署顺序依赖。
await app.MigrateDatabaseAsync(typeof(CartItem));

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();
app.Run();
