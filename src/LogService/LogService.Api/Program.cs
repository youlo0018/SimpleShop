using LogService.Api.Consumers;
using LogService.Api.Endpoints;
using LogService.Api;
using LogService.Api.Elasticsearch;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// 日志服务本身很轻：不对外承载业务，只需要健康检查和后台消费者。
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddSingleton<ElasticsearchLogWriter>();
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.Configure<ElasticsearchOptions>(builder.Configuration.GetSection(ElasticsearchOptions.SectionName));
builder.Services.AddHostedService<LoggingEventConsumer>();

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapGet("/metrics", async (HttpContext context) =>
{
    context.Response.ContentType = "text/plain; version=0.0.4";
    await Prometheus.Metrics.DefaultRegistry.CollectAndExportAsTextAsync(context.Response.Body, context.RequestAborted);
});
app.MapLogQueryEndpoint();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();
