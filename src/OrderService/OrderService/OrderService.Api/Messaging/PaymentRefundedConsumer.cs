using System.Text;
using System.Text.Json;
using CommunalService.Domain.Infrastructure.Locks;
using OrderService.Domain.IRepository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrderService.Api.Messaging;

public sealed class PaymentRefundedConsumer(
    IConfiguration configuration,
    IOrderRepository orderRepository,
    IDistributedLock distributedLock,
    ILogger<PaymentRefundedConsumer> logger) : BackgroundService
{
    private IConnection? _connection;
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await StartCoreAsync(stoppingToken);
                return;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "连接 RabbitMQ 失败，10 秒后重试。");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private async Task StartCoreAsync(CancellationToken cancellationToken)
    {
        var hostName = configuration.GetSection("RabbitMQ")["HostName"] ?? "localhost";
        var userName = configuration.GetSection("RabbitMQ")["UserName"] ?? "guest";
        var password = configuration.GetSection("RabbitMQ")["Password"] ?? "guest";
        var exchange = configuration.GetSection("RabbitMQ")["Exchange"] ?? "simpleshop.events";

        var factory = new ConnectionFactory { HostName = hostName, UserName = userName, Password = password };
        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await _channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);
        await _channel.QueueDeclareAsync("order.payment.refunded", durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await _channel.QueueBindAsync("order.payment.refunded", exchange, "payment.refunded", cancellationToken: cancellationToken);

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
                logger.LogError(exception, "处理退款事件失败。");
                await _channel.BasicNackAsync(eventArgs.DeliveryTag, false, requeue: true);
            }
        };
        await _channel.BasicConsumeAsync("order.payment.refunded", false, consumer, cancellationToken: cancellationToken);
    }

    private async Task HandleMessage(byte[] body, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(Encoding.UTF8.GetString(body));
        var root = document.RootElement.GetProperty("payload");
        var orderNo = root.GetProperty("bizNo").GetString() ?? string.Empty;
        var isAllRefund = root.GetProperty("isAllRefund").GetBoolean();
        if (string.IsNullOrWhiteSpace(orderNo))
        {
            return;
        }

        var existing = await orderRepository.GetByOrderNoAsync(orderNo, cancellationToken);
        if (existing is null || existing.IsAllRefund)
        {
            return;
        }

        await using var lockHandle = await distributedLock.AcquireAsync(
            $"lock:order:{existing.Id}", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(2), cancellationToken);
        if (lockHandle is null)
        {
            throw new TimeoutException($"获取订单退款锁超时:{orderNo}");
        }

        await orderRepository.TryApplyRefundAsync(orderNo, isAllRefund, cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null) await _channel.CloseAsync(cancellationToken);
        if (_connection is not null) await _connection.CloseAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
