using CommunalService.Domain.Infrastructure;
using CustomerService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
Console.WriteLine($"AgileConfig__appId = {Environment.GetEnvironmentVariable("AgileConfig__appId")}");
builder.Services.AddOpenApi();

 builder.AddInfrastructure();
 CustomerService.Application.DependencyInjection.AddInfrastructure(builder);
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
