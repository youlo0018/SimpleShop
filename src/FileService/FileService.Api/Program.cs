using CommunalService.Application.Common;
using CommunalService.Domain;
using FileService.Application;
using FileService.Domain.Entity;
using FileService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.AddBasicServices();
builder.AddInfrastructure();
builder.AddApplication();
builder.Services.AddControllers();
builder.AddMediatRWithHandlers(typeof(FileService.Application.DependencyInjection).Assembly, typeof(ValidationBehavior<,>).Assembly);

var app = builder.Build();
app.AddApplication();
await app.AddBaseInfrastructure();
// 文件服务自治管理元数据表（文件内容由存储后端负责）。
await app.MigrateDatabaseAsync(typeof(StoredFile));

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();
app.Run();
