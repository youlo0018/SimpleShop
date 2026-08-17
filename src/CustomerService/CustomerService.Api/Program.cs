using CommunalService.Application.Common;
using CommunalService.Domain;
using CustomerService.Application;
using CustomerService.Application.Features.Customer.GetCustomer;
using CustomerService.Infrastructure;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi



builder.Services.AddOpenApi();
builder.AddBasicServices();
builder.AddInfrastructure();
builder.Services.AddControllers();
builder.AddMediatRWithHandlers(typeof(GetCustomerCommand).Assembly, typeof(ValidationBehavior<,>).Assembly);
var app = builder.Build();
app.UseRouting();


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