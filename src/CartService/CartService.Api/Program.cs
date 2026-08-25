using CartService.Application;
using CommunalService.Domain;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.AddBasicServices();
builder.AddApplication();
builder.Services.AddControllers();

var app = builder.Build();
app.AddApplication();
await app.AddBaseInfrastructure();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();
app.Run();
