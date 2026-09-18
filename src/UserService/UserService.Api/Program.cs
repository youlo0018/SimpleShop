using CommunalService.Application.Common;
using CommunalService.Domain;
using CommunalService.Domain.Infrastructure;
using UserService.Application;
using UserService.Infrastructure;
using UserService.Domain.Entity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.AddInfrastructure();
builder.AddApplication();
builder.Services.AddControllers();
builder.AddMediatRWithHandlers(typeof(UserService.Application.DependencyInjection).Assembly, typeof(ValidationBehavior<,>).Assembly);

var app = builder.Build();

app.AddApplication();
await app.AddBaseInfrastructure();

// 后台账号表由当前服务自治；C 端客户账号在 CustomerService，两套账号体系完全分离。
app.Services.GetRequiredService<IFreeSql>().CodeFirst.SyncStructure<User>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => { options.SwaggerEndpoint("/openapi/v1.json", "My API V1"); });
}

app.MapControllers();

app.Run();
