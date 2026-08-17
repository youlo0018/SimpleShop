using AgileConfig.Client;
using CommunalService.Domain;
using CommunalService.Domain.Infrastructure;
using OrderService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.AddBasicServices();
builder.AddInfrastructure();
builder.Services.AddControllers();

var app = builder.Build();

await app.AddBaseInfrastructure();

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

