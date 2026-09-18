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
    /// <summary>事件 ID。</summary>
    public Guid EventId { get; set; }

    /// <summary>事件发生时间。</summary>
    public DateTimeOffset OccurredAt { get; set; }

    /// <summary>链路追踪 ID。</summary>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>平台 ID。</summary>
    public long PlatformId { get; set; }

    /// <summary>商户 ID。</summary>
    public long MerchantId { get; set; }

    /// <summary>用户 ID（网关登录态注入）。</summary>
    public long UserId { get; set; }

    /// <summary>页码（从 1 开始）。</summary>
    public string Page { get; set; } = string.Empty;

    /// <summary>条目 ID。</summary>
    public long ItemId { get; set; }

    /// <summary>商品 SKU ID。</summary>
    public long SkuId { get; set; }

    /// <summary>来源。</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>停留毫秒数。</summary>
    public int StayMilliseconds { get; set; }

    /// <summary>服务名。</summary>
    public string Service { get; set; } = string.Empty;

    /// <summary>运行环境。</summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>服务版本号。</summary>
    public string ServiceVersion { get; set; } = string.Empty;
}

/// <summary>业务操作日志文档。</summary>
public sealed class OperationLog
{
    /// <summary>事件 ID。</summary>
    public Guid EventId { get; set; }

    /// <summary>事件发生时间。</summary>
    public DateTimeOffset OccurredAt { get; set; }

    /// <summary>链路追踪 ID。</summary>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>平台 ID。</summary>
    public long PlatformId { get; set; }

    /// <summary>商户 ID。</summary>
    public long MerchantId { get; set; }

    /// <summary>操作员 ID。</summary>
    public long OperatorId { get; set; }

    /// <summary>操作类型。</summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>操作对象类型。</summary>
    public string ObjectType { get; set; } = string.Empty;

    /// <summary>操作对象 ID。</summary>
    public string ObjectId { get; set; } = string.Empty;

    /// <summary>操作员类型（平台/商户/客户/系统）。</summary>
    public string OperatorType { get; set; } = string.Empty;

    /// <summary>操作摘要（脱敏后）。</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>客户端 IP（脱敏后）。</summary>
    public string Ip { get; set; } = string.Empty;

    /// <summary>服务名。</summary>
    public string Service { get; set; } = string.Empty;

    /// <summary>运行环境。</summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>服务版本号。</summary>
    public string ServiceVersion { get; set; } = string.Empty;
}

/// <summary>异常日志文档。</summary>
public sealed class ExceptionLog
{
    /// <summary>事件 ID。</summary>
    public Guid EventId { get; set; }

    /// <summary>事件发生时间。</summary>
    public DateTimeOffset OccurredAt { get; set; }

    /// <summary>链路追踪 ID。</summary>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>平台 ID。</summary>
    public long PlatformId { get; set; }

    /// <summary>商户 ID。</summary>
    public long MerchantId { get; set; }

    /// <summary>用户 ID（网关登录态注入）。</summary>
    public long UserId { get; set; }

    /// <summary>请求路径。</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>HTTP 方法。</summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>HTTP 状态码。</summary>
    public int StatusCode { get; set; }

    /// <summary>异常类型。</summary>
    public string ExceptionType { get; set; } = string.Empty;

    /// <summary>消息内容。</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>异常堆栈（脱敏后）。</summary>
    public string StackTrace { get; set; } = string.Empty;

    /// <summary>服务名。</summary>
    public string Service { get; set; } = string.Empty;

    /// <summary>运行环境。</summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>服务版本号。</summary>
    public string ServiceVersion { get; set; } = string.Empty;
}
