using ScheduledService;
using FreeSql;
using ScheduledService.Compensation;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddScheduledJobs(
    builder.Configuration);

var app = builder.Build();

// 补偿表由定时项目自治；订单库结构仍归订单服务管理。
app.Services.GetRequiredKeyedService<IFreeSql>("scheduled").CodeFirst.SyncStructure<PendingStockRelease>();

await app.RunAsync();
