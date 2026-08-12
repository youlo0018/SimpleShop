using CommunalService.Application.Common;
using CommunalService.Domain.Contracts.Services;
using CommunalService.Domain.Infrastructure;
using CustomerService.Application;
using CustomerService.Application.Features.Customer.GetCustomer;
using CustomerService.Infrastructure;
using Grpc.AspNetCore.Server;
using Microsoft.AspNetCore.Server.Kestrel.Core;



var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5280);
    options.ListenAnyIP(5001, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2; // 强制 HTTP/2
    });
});

builder.Services.AddOpenApi();
builder.AddInfrastructure();
builder.Services.AddControllers();
builder.AddMediatRWithHandlers(typeof(GetCustomerCommand).Assembly, typeof(ValidationBehavior<,>).Assembly);
var app = builder.Build();
app.UseRouting();


app.AddApplicationInfrastructure();
await app.AddBaseInfrastructure();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => { options.SwaggerEndpoint("/openapi/v1.json", "My API V1"); });
}

app.UseHttpsRedirection();
var options = app.Services;

app.MapControllers();
app.Run();