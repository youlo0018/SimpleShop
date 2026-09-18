namespace CommunalService.Domain.Messaging;

/// <summary>
/// 消息发布抽象：topic 即 RabbitMQ Topic 交换机的 routing key（如 payment.succeeded）；
/// key 作为消息的业务键（订单号/退款单号），便于排障追踪。实现见 RabbitMqMessagePublisher（懒连接）。
/// </summary>
public interface IMessagePublisher
{
    Task PublishAsync<TPayload>(
        string topic,
        string key,
        MessageEnvelope<TPayload> message,
        CancellationToken cancellationToken = default);
}
