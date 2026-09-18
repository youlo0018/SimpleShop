namespace CommunalService.Domain.Messaging;

/// <summary>
/// 全部 MQ 消息的统一信封：EventId 用作消费端幂等键（如 LogService 写 ES 的 _id）；
/// PlatformId/MerchantId/UserId 供下游做租户过滤与审计；TraceId 串联网关请求与异步链路；
/// PayloadVersion 为消费者声明兼容矩阵预留；Service/Environment/ServiceVersion 由日志发布器自动补齐。
/// 序列化统一 camelCase（发布端 JsonSerializerDefaults.Web），消费端读取 payload 需大小写一致。
/// </summary>
public sealed record MessageEnvelope<TPayload>(
    Guid EventId,
    string EventType,
    DateTimeOffset OccurredAt,
    string TraceId,
    long PlatformId,
    long MerchantId,
    long UserId,
    int PayloadVersion,
    TPayload Payload,
    string Service = "",
    string Environment = "",
    string ServiceVersion = "");
