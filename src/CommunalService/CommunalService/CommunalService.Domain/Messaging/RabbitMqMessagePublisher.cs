using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace CommunalService.Domain.Messaging;

/// <summary>
/// RabbitMQ 发布器。采用懒连接：应用启动不依赖 MQ；真正发消息时才建立连接。
/// </summary>
public sealed class RabbitMqMessagePublisher : IMessagePublisher, IDisposable
{
    /// <summary>消息序列化选项（camelCase，发布/消费两端一致）。</summary>
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>RabbitMQ 连接工厂（配置来自 RabbitMqOptions）。</summary>
    private readonly IConnectionFactory _factory;
    /// <summary>事件交换机名（所有业务事件统一发布到此交换机）。</summary>
    private readonly string _exchange;
    /// <summary>连接建立互斥锁：避免并发请求重复建连。</summary>
    private readonly SemaphoreSlim _connectLock = new(1, 1);
    /// <summary>复用的 RabbitMQ 连接（懒加载）。</summary>
    private IConnection? _connection;
    /// <summary>发布通道（连接断开后重建）。</summary>
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

    /// <summary>查询：GetChannelAsync。</summary>
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
        // 消息契约统一 camelCase，和 REST API 保持一致，避免生产者与消费者大小写漂移。
        var body = JsonSerializer.SerializeToUtf8Bytes(message, JsonOptions);
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

    /// <summary>释放资源。</summary>
    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        _connectLock.Dispose();
    }
}
