using CommunalService.Application.Common;
using CommunalService.Domain;
using CustomerService.Application;
using CustomerService.Application.Features.Customer.GetProfile;
using CustomerService.Infrastructure;
using FreeSql;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi



builder.Services.AddOpenApi();
builder.AddBasicServices();
builder.AddInfrastructure();
builder.AddApplication();
builder.Services.AddControllers();
builder.AddMediatRWithHandlers(typeof(GetProfileQuery).Assembly, typeof(ValidationBehavior<,>).Assembly);
var app = builder.Build();
app.UseRouting();


app.AddApplication();
CustomerService.Infrastructure.DatabaseInitializer.Initialize(app.Services.GetRequiredService<IFreeSql>());
await app.AddBaseInfrastructure();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => { options.SwaggerEndpoint("/openapi/v1.json", "My API V1"); });
}

app.MapControllers();
app.Run();
