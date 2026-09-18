namespace CommunalService.Domain.Logging;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

/// <summary>
/// 默认实现：从请求上下文里取平台、商户、操作人和 TraceId。
/// 日志发布失败只写 Warning；审计日志不能反过来拖垮正在执行的业务。
/// </summary>
internal sealed class OperationLogger(
    LoggingEventPublisher logPublisher,
    ILogger<OperationLogger> logger) : IOperationLogger
{
    /// <summary>内部处理：LogAsync。</summary>
    public async Task LogAsync(
        HttpContext httpContext,
        string operationType,
        string objectType,
        string objectId,
        string summary,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await logPublisher.PublishOperationAsync(
                ParseLong(httpContext.Request.Headers, "X-Platform-Id"),
                ParseLong(httpContext.Request.Headers, "X-Merchant-Id"),
                ParseLong(httpContext.Request.Headers, "X-User-Id"),
                httpContext.TraceIdentifier,
                new OperationLogEvent
                {
                    OperationType = operationType,
                    ObjectType = objectType,
                    ObjectId = objectId,
                    OperatorType = "user",
                    Summary = summary,
                    Ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty
                },
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "业务操作日志发布失败，已忽略。");
        }
    }

    /// <summary>辅助处理：ParseLong。</summary>
    private static long ParseLong(IHeaderDictionary headers, string name)
        => long.TryParse(headers[name].ToString(), out var value) ? value : 0;
}
