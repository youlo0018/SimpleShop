namespace CommunalService.Domain.Messaging;

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
