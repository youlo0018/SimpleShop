using System.Text;
using System.Text.Json;
using CommunalService.Domain.Infrastructure.Locks;
using Microsoft.Extensions.Configuration;
using OrderService.Domain.IRepository;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrderService.Api.Messaging;

/// <summary>
/// 订单侧监听支付成功消息，保证支付回调丢失时订单状态也能最终一致。
/// </summary>
public sealed class PaymentSucceededConsumer(
    IConfiguration configuration,
    IOrderRepository orderRepository,
    IDistributedLock distributedLock,
    ILogger<PaymentSucceededConsumer> logger) : BackgroundService
{
    private IConnection? _connection;
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await StartConsumerAsync(stoppingToken);
                return;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "连接 RabbitMQ 失败，10 秒后重试。");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private async Task StartConsumerAsync(CancellationToken cancellationToken)
    {
        var section = configuration.GetSection("RabbitMQ");
        var factory = new ConnectionFactory
        {
            HostName = section["HostName"] ?? "localhost",
            UserName = section["UserName"] ?? "guest",
            Password = section["Password"] ?? "guest"
        };
        var exchange = section["Exchange"] ?? "simpleshop.events";

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await _channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);
        await _channel.QueueDeclareAsync("order.payment.succeeded", durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await _channel.QueueBindAsync("order.payment.succeeded", exchange, "payment.succeeded", cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            try
            {
                await HandleMessage(eventArgs.Body.ToArray(), eventArgs.CancellationToken);
                await _channel.BasicAckAsync(eventArgs.DeliveryTag, false);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "处理支付成功事件失败。");
                await _channel.BasicNackAsync(eventArgs.DeliveryTag, false, requeue: exception is LockTimeoutException);
            }
        };

        await _channel.BasicConsumeAsync("order.payment.succeeded", autoAck: false, consumer, cancellationToken: cancellationToken);
    }

    private async Task HandleMessage(byte[] body, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(Encoding.UTF8.GetString(body));
        var payload = document.RootElement.GetProperty("payload");
        var orderNo = payload.GetProperty("bizNo").GetString();

        if (string.IsNullOrWhiteSpace(orderNo))
        {
            return;
        }

        var existing = await orderRepository.GetByOrderNoAsync(orderNo, cancellationToken);
        if (existing is null)
        {
            return;
        }

        // 支付和超时关单必须竞争同一把订单状态锁；不同锁键会让两个流程互相覆盖。
        await using var lockHandle = await distributedLock.AcquireAsync(
            $"lock:order:{existing.Id}",
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(2),
            cancellationToken);

        if (lockHandle is null)
        {
            logger.LogWarning("获取订单支付锁失败，消息将重新入队。OrderNo：{OrderNo}。", orderNo);
            throw new LockTimeoutException(orderNo);
        }

        // 锁内重新读取状态，避免用锁前的旧状态覆盖超时关单结果。
        var latest = await orderRepository.GetByOrderNoAsync(orderNo, cancellationToken);
        if (latest is null || latest.OrderStatus != (int)Domain.Entity.OrderState.AwaitPayment || latest.IsPayment)
        {
            return;
        }

        await orderRepository.TryMarkPaidAsync(latest.OrderNo, DateTime.Now, cancellationToken);
    }

    private sealed class LockTimeoutException(string orderNo)
        : Exception($"获取订单支付锁超时:{orderNo}");

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null) await _channel.CloseAsync(cancellationToken);
        if (_connection is not null) await _connection.CloseAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
