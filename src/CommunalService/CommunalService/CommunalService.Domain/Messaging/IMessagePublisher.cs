namespace CommunalService.Domain.Messaging;

public interface IMessagePublisher
{
    Task PublishAsync<TPayload>(
        string topic,
        string key,
        MessageEnvelope<TPayload> message,
        CancellationToken cancellationToken = default);
}
