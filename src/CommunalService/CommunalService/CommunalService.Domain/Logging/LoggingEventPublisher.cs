using System.Reflection;
using CommunalService.Domain.Messaging;

namespace CommunalService.Domain.Logging;

/// <summary>
/// 日志统一出口：业务代码只管“发生了什么”，这里负责补齐服务身份、选择 Topic 和做敏感信息清洗。
/// MQ 不可用时的异常由调用方兜底记录，不能反过来影响业务主流程。
/// </summary>
public sealed class LoggingEventPublisher(IMessagePublisher messagePublisher)
{
    private static string ServiceName => Assembly.GetEntryAssembly()?.GetName().Name ?? "SimpleShop";
    private static string EnvironmentName => Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
    private static string ServiceVersion => Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown";

    public Task PublishPageViewAsync(
        long platformId,
        long merchantId,
        long userId,
        string traceId,
        PageViewEvent pageView,
        CancellationToken cancellationToken = default)
    {
        return PublishAsync("pv.log", userId.ToString(), "pv.page_view", traceId, platformId, merchantId, userId, pageView, cancellationToken);
    }

    public Task PublishOperationAsync(
        long platformId,
        long merchantId,
        long operatorId,
        string traceId,
        OperationLogEvent operation,
        CancellationToken cancellationToken = default)
    {
        var sanitized = operation with { Summary = SensitiveDataSanitizer.Sanitize(operation.Summary) };

        return PublishAsync("operation.log", $"{operatorId}:{sanitized.ObjectType}", "business.operation", traceId, platformId, merchantId, operatorId, sanitized, cancellationToken);
    }

    public Task PublishExceptionAsync(
        long platformId,
        long merchantId,
        long userId,
        string traceId,
        ExceptionLogEvent exception,
        CancellationToken cancellationToken = default)
    {
        // 堆栈里最容易夹带连接串、Token 和用户资料，入库前统一擦一遍。
        var sanitized = exception with
        {
            Message = SensitiveDataSanitizer.Sanitize(exception.Message),
            StackTrace = SensitiveDataSanitizer.Sanitize(exception.StackTrace)
        };

        return PublishAsync("exception.log", $"{sanitized.Service}:{sanitized.Path}", "system.exception", traceId, platformId, merchantId, userId, sanitized, cancellationToken);
    }

    private Task PublishAsync<TPayload>(
        string routingKey,
        string routingValue,
        string eventType,
        string traceId,
        long platformId,
        long merchantId,
        long userId,
        TPayload payload,
        CancellationToken cancellationToken)
    {
        var envelope = new MessageEnvelope<TPayload>(
            Guid.NewGuid(),
            eventType,
            DateTimeOffset.UtcNow,
            traceId,
            platformId,
            merchantId,
            userId,
            1,
            payload,
            ServiceName,
            EnvironmentName,
            ServiceVersion);

        return messagePublisher.PublishAsync(routingKey, routingValue, envelope, cancellationToken);
    }
}
