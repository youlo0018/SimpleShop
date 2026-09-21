using System.Diagnostics;
using CommunalService.Domain.Logging;
using CommunalService.Domain.Messaging;

namespace SimpleShop.Gateway;

/// <summary>
/// 网关访问日志：把每个请求转成一条 PV/访问事件，异步丢给消息队列。
/// 日志失败不能影响用户请求，所以这里吞掉异常只记录警告。
/// </summary>
public sealed class LoggingEventMiddleware(
    RequestDelegate next,
    LoggingEventPublisher logPublisher,
    ILogger<LoggingEventMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var startedAt = Stopwatch.GetTimestamp();

        try
        {
            await next(context);
        }
        finally
        {
            try
            {
                var elapsed = (int)Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
                await logPublisher.PublishPageViewAsync(
                    ParseLong(context.Request.Headers["X-Platform-Id"]),
                    ParseLong(context.Request.Headers["X-Merchant-Id"]),
                    ParseLong(context.Request.Headers["X-User-Id"]),
                    context.Response.Headers["X-Request-Id"].ToString(),
                    new PageViewEvent
                    {
                        Page = context.Request.Path,
                        Source = "gateway",
                        StayMilliseconds = elapsed
                    });
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "发布网关访问日志失败，请求不受影响。");
            }
        }
    }

    private static long ParseLong(string value)
        => long.TryParse(value, out var result) ? result : 0;
}
