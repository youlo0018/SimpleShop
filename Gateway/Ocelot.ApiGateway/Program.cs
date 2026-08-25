using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Microsoft.Extensions.Options;
using System.Text;
using CommunalService.Domain.Logging;
using CommunalService.Domain.Messaging;
using SimpleShop.Gateway.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.AddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();
builder.Services.AddSingleton<LoggingEventPublisher>();
builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<GrayReleaseContextMiddleware>();
app.UseMiddleware<LoggingEventMiddleware>();
app.Use(async (context, next) =>
{
    if (context.Request.Path.Equals("/health", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status200OK;
        await context.Response.WriteAsJsonAsync(new { status = "Healthy" });
        return;
    }

    await next();
});
await app.UseOcelot();
app.Run();
