using CommunalService.Application.Common;
using CommunalService.Domain.Infrastructure;
using CommunalService.Domain;
using UserService.Application;
using UserService.Infrastructure;
using UserService.Domain.Entity;

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

// 用户表由当前服务自治；启动时同步新增角色和头像字段，便于后台与应用端共用账号体系。
app.Services.GetRequiredService<IFreeSql>().CodeFirst.SyncStructure<User>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => { options.SwaggerEndpoint("/openapi/v1.json", "My API V1"); });
}

app.MapControllers();

app.Run();
