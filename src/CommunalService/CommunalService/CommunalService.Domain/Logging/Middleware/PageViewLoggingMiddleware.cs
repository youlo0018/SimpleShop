using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using CommunalService.Domain.Logging;

namespace CommunalService.Domain.Logging.Middleware;

/// <summary>
/// PV 埋点：每个 API 请求结束后记录页面/接口、耗时和用户身份。
/// 日志失败只降级为 Warning，绝不能让“记录访问”影响业务响应。
/// </summary>
public sealed class PageViewLoggingMiddleware(
    RequestDelegate next,
    LoggingEventPublisher logPublisher,
    ILogger<PageViewLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            try
            {
                var request = context.Request;
                // 网关已经记录过入口 PV 时，下游服务带标记跳过，避免同一次访问重复计数。
                var isGatewayRecorded = request.Headers.ContainsKey("X-Gateway-PV");
                if (request.Path.StartsWithSegments("/api") && !isGatewayRecorded && !IsInternalPath(request.Path))
                {
                    var platformId = ParseLong(context, "X-Platform-Id");
                    var merchantId = ParseLong(context, "X-Merchant-Id");
                    var userId = ParseLong(context, "X-User-Id");
                    var traceId = context.TraceIdentifier;

                    await logPublisher.PublishPageViewAsync(
                        platformId,
                        merchantId,
                        userId,
                        traceId,
                        new PageViewEvent
                        {
                            Page = request.Path.ToString(),
                            Source = request.Headers["Referer"].ToString(),
                            StayMilliseconds = (int)stopwatch.Elapsed.TotalMilliseconds
                        },
                        CancellationToken.None);
                }
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "PV 日志发布失败，已忽略。");
            }
        }
    }

    private static long ParseLong(HttpContext context, string header)
        => long.TryParse(context.Request.Headers[header].ToString(), out var value) ? value : 0;

    private static bool IsInternalPath(PathString path)
        => path.StartsWithSegments("/api/log") || path.StartsWithSegments("/health") || path.StartsWithSegments("/metrics");
}
