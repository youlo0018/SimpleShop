namespace CommunalService.Domain.Logging.Middleware;

using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

/// <summary>
/// 全局异常兜底：未处理异常先发一条可排障的日志，再统一告诉用户“系统繁忙”。
/// 发布日志自身失败也不能二次抛出，否则会把真正的业务异常盖掉。
/// </summary>
public sealed class ExceptionLoggingMiddleware(
    RequestDelegate next,
    LoggingEventPublisher logPublisher,
    ILogger<ExceptionLoggingMiddleware> logger)
{
    /// <summary>内部处理：InvokeAsync。</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException exception)
        {
            var errors = exception.Errors
                .Where(error => !string.IsNullOrWhiteSpace(error.ErrorMessage))
                .GroupBy(error => error.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).Distinct().ToArray());
            var messages = errors.Values.SelectMany(values => values).Distinct().ToArray();
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsJsonAsync(new
            {
                code = 400,
                message = messages.Length > 0 ? string.Join("；", messages) : "输入验证失败",
                errors
            });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "请求处理失败：{Method} {Path}", context.Request.Method, context.Request.Path);
            await PublishAsync(context, exception);

            // 统一错误契约：TraceId 给用户，堆栈只进 Elasticsearch，不暴露给前端。
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsJsonAsync(new
            {
                code = 500,
                message = "系统繁忙，请稍后重试。",
                traceId = context.TraceIdentifier
            });
        }
    }

    /// <summary>辅助处理：PublishAsync。</summary>
    private async Task PublishAsync(HttpContext context, Exception exception)
    {
        try
        {
            var request = context.Request;
            await logPublisher.PublishExceptionAsync(
                ParseLong(request.Headers, "X-Platform-Id"),
                ParseLong(request.Headers, "X-Merchant-Id"),
                ParseLong(request.Headers, "X-User-Id"),
                context.TraceIdentifier,
                new ExceptionLogEvent
                {
                    Service = Environment.MachineName,
                    Path = request.Path.ToString(),
                    Method = request.Method,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    ExceptionType = exception.GetType().Name,
                    Message = exception.Message,
                    StackTrace = exception.StackTrace ?? string.Empty
                },
                CancellationToken.None);
        }
        catch (Exception loggingFailure)
        {
            logger.LogError(loggingFailure, "异常日志发布失败；原始异常：{Message}", exception.Message);
        }
    }

    /// <summary>辅助处理：ParseLong。</summary>
    private static long ParseLong(IHeaderDictionary headers, string name)
        => long.TryParse(headers[name].ToString(), out var value) ? value : 0;
}
