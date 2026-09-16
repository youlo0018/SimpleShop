using System.Text;
using System.Text.Json;
using MarketingService.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MarketingService.Application.Messaging;

/// <summary>
/// 支付成功消费者：发放满赠券（营销记录已在订单落库时写入，这里只处理赠券）。
/// 独立队列绑定 payment.succeeded，与订单/库存消费者互不影响。
/// </summary>
public sealed class PaymentSucceededMarketingConsumer(
    IConfiguration configuration,
    MarketingCommitService commitService,
    ILogger<PaymentSucceededMarketingConsumer> logger) : BackgroundService
{
    private IConnection? _connection;
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await StartAsync(stoppingToken);
                return;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "连接 RabbitMQ 失败，10 秒后重试。");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private async Task StartAsync(CancellationToken cancellationToken)
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
        await _channel.QueueDeclareAsync("marketing.payment.succeeded", durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await _channel.QueueBindAsync("marketing.payment.succeeded", exchange, "payment.succeeded", cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            try
            {
                await HandlePaymentSucceededAsync(eventArgs.Body.ToArray(), eventArgs.CancellationToken);
                await _channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "处理支付成功事件失败。");
                await _channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false);
            }
        };
        await _channel.BasicConsumeAsync("marketing.payment.succeeded", autoAck: false, consumer, cancellationToken: cancellationToken);
    }

    private async Task HandlePaymentSucceededAsync(byte[] body, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(Encoding.UTF8.GetString(body));
        if (!document.RootElement.TryGetProperty("payload", out var payload)) return;
        var bizNo = payload.TryGetProperty("bizNo", out var value) ? value.GetString() : null;
        if (string.IsNullOrWhiteSpace(bizNo)) return;
        await commitService.IssueGiftCouponsAsync(bizNo, cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null) await _channel.CloseAsync(cancellationToken);
        if (_connection is not null) await _connection.CloseAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}

/// <summary>
/// 订单取消消费者：支付超时关单/主动取消时回退券占用，避免券被未成交订单长期占用。
/// </summary>
public sealed class OrderCancelledMarketingConsumer(
    IConfiguration configuration,
    MarketingCommitService commitService,
    ILogger<OrderCancelledMarketingConsumer> logger) : BackgroundService
{
    private IConnection? _connection;
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await StartAsync(stoppingToken);
                return;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "连接 RabbitMQ 失败，10 秒后重试。");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private async Task StartAsync(CancellationToken cancellationToken)
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
        await _channel.QueueDeclareAsync("marketing.order.cancelled", durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await _channel.QueueBindAsync("marketing.order.cancelled", exchange, "order.cancelled", cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            try
            {
                await HandleOrderCancelledAsync(eventArgs.Body.ToArray(), eventArgs.CancellationToken);
                await _channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "处理订单取消事件失败。");
                await _channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false);
            }
        };
        await _channel.BasicConsumeAsync("marketing.order.cancelled", autoAck: false, consumer, cancellationToken: cancellationToken);
    }

    private async Task HandleOrderCancelledAsync(byte[] body, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(Encoding.UTF8.GetString(body));
        if (!document.RootElement.TryGetProperty("payload", out var payload)) return;
        var orderNo = payload.TryGetProperty("orderNo", out var value) ? value.GetString() : null;
        if (string.IsNullOrWhiteSpace(orderNo)) return;
        await commitService.ReleaseAsync(orderNo, cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null) await _channel.CloseAsync(cancellationToken);
        if (_connection is not null) await _connection.CloseAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
