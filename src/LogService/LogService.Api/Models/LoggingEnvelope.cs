using System.Text.Json.Serialization;

namespace LogService.Api.Models;

/// <summary>
/// 统一日志信封：公共字段用于 Kibana 过滤，Payload 按 EventType 反序列化。
/// </summary>
public sealed class LoggingEnvelope<TPayload>
{
    [JsonPropertyName("eventId")] public Guid EventId { get; set; }

    [JsonPropertyName("eventType")] public string EventType { get; set; } = string.Empty;

    [JsonPropertyName("occurredAt")] public DateTimeOffset OccurredAt { get; set; }

    [JsonPropertyName("traceId")] public string TraceId { get; set; } = string.Empty;

    [JsonPropertyName("platformId")] public long PlatformId { get; set; }

    [JsonPropertyName("merchantId")] public long MerchantId { get; set; }

    [JsonPropertyName("userId")] public long UserId { get; set; }

    [JsonPropertyName("service")] public string Service { get; set; } = string.Empty;

    [JsonPropertyName("environment")] public string Environment { get; set; } = string.Empty;

    [JsonPropertyName("serviceVersion")] public string ServiceVersion { get; set; } = string.Empty;

    [JsonPropertyName("payloadVersion")] public int PayloadVersion { get; set; }

    [JsonPropertyName("payload")] public TPayload Payload { get; set; } = default!;
}

/// <summary>PV 日志文档。</summary>
public sealed class PageViewLog
{
    public Guid EventId { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public string TraceId { get; set; } = string.Empty;

    public long PlatformId { get; set; }

    public long MerchantId { get; set; }

    public long UserId { get; set; }

    public string Page { get; set; } = string.Empty;

    public long ItemId { get; set; }

    public long SkuId { get; set; }

    public string Source { get; set; } = string.Empty;

    public int StayMilliseconds { get; set; }

    public string Service { get; set; } = string.Empty;

    public string Environment { get; set; } = string.Empty;

    public string ServiceVersion { get; set; } = string.Empty;
}

/// <summary>业务操作日志文档。</summary>
public sealed class OperationLog
{
    public Guid EventId { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public string TraceId { get; set; } = string.Empty;

    public long PlatformId { get; set; }

    public long MerchantId { get; set; }

    public long OperatorId { get; set; }

    public string OperationType { get; set; } = string.Empty;

    public string ObjectType { get; set; } = string.Empty;

    public string ObjectId { get; set; } = string.Empty;

    public string OperatorType { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string Ip { get; set; } = string.Empty;

    public string Service { get; set; } = string.Empty;

    public string Environment { get; set; } = string.Empty;

    public string ServiceVersion { get; set; } = string.Empty;
}

/// <summary>异常日志文档。</summary>
public sealed class ExceptionLog
{
    public Guid EventId { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public string TraceId { get; set; } = string.Empty;

    public long PlatformId { get; set; }

    public long MerchantId { get; set; }

    public long UserId { get; set; }

    public string Path { get; set; } = string.Empty;

    public string Method { get; set; } = string.Empty;

    public int StatusCode { get; set; }

    public string ExceptionType { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string StackTrace { get; set; } = string.Empty;

    public string Service { get; set; } = string.Empty;

    public string Environment { get; set; } = string.Empty;

    public string ServiceVersion { get; set; } = string.Empty;
}
