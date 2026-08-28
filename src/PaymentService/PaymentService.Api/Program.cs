using CommunalService.Application.Common;
using CommunalService.Domain;
using PaymentService.Application;
using PaymentService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.AddBasicServices();
builder.AddInfrastructure();
builder.Services.AddControllers();
builder.AddMediatRWithHandlers(
    typeof(PaymentService.Application.DependencyInjection).Assembly,
    typeof(ValidationBehavior<,>).Assembly);

var app = builder.Build();
app.AddApplication();
await app.AddBaseInfrastructure();
await app.MigrateDatabaseAsync(
    typeof(PaymentService.Domain.Entity.PaymentOrder),
    typeof(PaymentService.Domain.Entity.RefundOrder),
    typeof(PaymentService.Domain.Entity.RefundOrderItem));

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();
app.Run();
