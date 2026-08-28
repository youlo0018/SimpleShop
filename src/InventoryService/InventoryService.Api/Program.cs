using CommunalService.Application.Common;
using CommunalService.Domain;
using InventoryService.Application;
using InventoryService.Application.Messaging;
using InventoryService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.AddBasicServices();
builder.AddInfrastructure();
builder.Services.AddControllers();
builder.AddMediatRWithHandlers(
    typeof(InventoryService.Application.DependencyInjection).Assembly,
    typeof(ValidationBehavior<,>).Assembly);
builder.Services.AddHostedService<PaymentSucceededConsumer>();
builder.Services.AddHostedService<ProductCreatedConsumer>();
builder.Services.AddHostedService<PaymentRefundedConsumer>();

var app = builder.Build();
app.AddApplication();
await app.AddBaseInfrastructure();
await app.MigrateDatabaseAsync(
    typeof(InventoryService.Domain.Entity.Stock),
    typeof(InventoryService.Domain.Entity.StockFlow));

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();
app.Run();
