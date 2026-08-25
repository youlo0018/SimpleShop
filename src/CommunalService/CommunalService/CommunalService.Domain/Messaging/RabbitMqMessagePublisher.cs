using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace CommunalService.Domain.Messaging;

/// <summary>
/// RabbitMQ 发布器。采用懒连接：应用启动不依赖 MQ；真正发消息时才建立连接。
/// </summary>
public sealed class RabbitMqMessagePublisher : IMessagePublisher, IDisposable
{
    private readonly IConnectionFactory _factory;
    private readonly string _exchange;
    private readonly SemaphoreSlim _connectLock = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqMessagePublisher(IOptions<RabbitMqOptions> options)
    {
        var settings = options.Value;
        _exchange = settings.Exchange;
        _factory = new ConnectionFactory
        {
            HostName = settings.HostName,
            UserName = settings.UserName,
            Password = settings.Password,
            AutomaticRecoveryEnabled = true
        };
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        await _connectLock.WaitAsync(cancellationToken);
        try
        {
            if (_channel is null)
            {
                // 懒连接：应用可以先启动；MQ 恢复后再继续发日志和事件。
                _connection = await _factory.CreateConnectionAsync(cancellationToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
                await _channel.ExchangeDeclareAsync(
                    _exchange,
                    ExchangeType.Topic,
                    durable: true,
                    autoDelete: false,
                    cancellationToken: cancellationToken);
            }

            return _channel;
        }
        finally
        {
            _connectLock.Release();
        }
    }

    public async Task PublishAsync<TPayload>(
        string topic,
        string key,
        MessageEnvelope<TPayload> message,
        CancellationToken cancellationToken = default)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(message);
        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = message.EventId.ToString(),
            Type = message.EventType,
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };

        var channel = await GetChannelAsync(cancellationToken);
        await channel.BasicPublishAsync(
            _exchange,
            topic,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        _connectLock.Dispose();
    }
}
