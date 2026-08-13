using CommunalService.Application.Common;
using CommunalService.Domain.Infrastructure;
using UserService.Application;
using UserService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.AddInfrastructure();
builder.Services.AddControllers();
builder.AddMediatRWithHandlers(typeof(UserService.Application.DependencyInjection).Assembly, typeof(ValidationBehavior<,>).Assembly);

var app = builder.Build();

app.AddApplication();
await app.AddBaseInfrastructure();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => { options.SwaggerEndpoint("/openapi/v1.json", "My API V1"); });
}

app.UseHttpsRedirection();


app.MapControllers();

app.Run();

