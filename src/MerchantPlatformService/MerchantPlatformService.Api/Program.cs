using CommunalService.Application.Common;
using CommunalService.Domain;
using MerchantPlatformService.Application;
using MerchantPlatformService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.AddBasicServices();
builder.AddInfrastructure();
builder.Services.AddControllers();
builder.AddMediatRWithHandlers(
    typeof(MerchantPlatformService.Application.DependencyInjection).Assembly,
    typeof(ValidationBehavior<,>).Assembly);

var app = builder.Build();

app.AddApplication();
await app.AddBaseInfrastructure();
await app.MigrateDatabaseAsync(
    typeof(MerchantPlatformService.Domain.Entity.Platform),
    typeof(MerchantPlatformService.Domain.Entity.Merchant),
    typeof(MerchantPlatformService.Domain.Entity.PlatformConfig),
    typeof(MerchantPlatformService.Domain.Entity.PlatformAppConfig));

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "MerchantPlatform API V1"));
}

if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();
app.MapControllers();
app.Run();
