using CommunalService.Application.Common;
using CommunalService.Domain;
using ProductService.Application;
using ProductService.Application.Features.Product.CreateProduct;
using ProductService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.AddBasicServices();
builder.AddInfrastructure();
builder.Services.AddControllers();
builder.AddMediatRWithHandlers(typeof(CreateProductCommand).Assembly, typeof(ValidationBehavior<,>).Assembly);

var app = builder.Build();
app.UseRouting();

app.AddApplication();
await app.AddBaseInfrastructure();
await app.MigrateDatabaseAsync(
    typeof(ProductService.Domain.Entity.Product),
    typeof(ProductService.Domain.Entity.Sku),
    typeof(ProductService.Domain.Entity.Category),
    typeof(ProductService.Domain.Entity.Brand),
    typeof(ProductService.Domain.Entity.UploadedFile));

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();
app.MapControllers();
app.Run();
