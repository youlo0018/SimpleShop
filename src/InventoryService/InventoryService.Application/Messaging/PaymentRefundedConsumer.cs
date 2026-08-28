using System.Text;
using System.Text.Json;
using CommunalService.Domain.Infrastructure.Locks;
using InventoryService.Domain.IRepository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace InventoryService.Application.Messaging;

public sealed class PaymentRefundedConsumer(
    IConfiguration configuration,
    IStockRepository stockRepository,
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
        await _channel.QueueDeclareAsync("inventory.payment.refunded", durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await _channel.QueueBindAsync("inventory.payment.refunded", exchange, "payment.refunded", cancellationToken: cancellationToken);

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
                logger.LogError(exception, "处理退款库存恢复事件失败。");
                await _channel.BasicNackAsync(eventArgs.DeliveryTag, false, requeue: true);
            }
        };
        await _channel.BasicConsumeAsync("inventory.payment.refunded", false, consumer, cancellationToken: cancellationToken);
    }

    private async Task HandleMessage(byte[] body, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(Encoding.UTF8.GetString(body));
        var root = document.RootElement.GetProperty("payload");
        var bizNo = root.GetProperty("refundNo").GetString() ?? string.Empty;
        foreach (var item in root.GetProperty("stockItems").EnumerateArray())
        {
            var skuId = item.GetProperty("skuId").GetInt64();
            var quantity = item.GetProperty("quantity").GetInt32();
            await using var lockHandle = await distributedLock.AcquireAsync(
                $"lock:stock:{skuId}", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(2), cancellationToken);
            if (lockHandle is null)
            {
                throw new TimeoutException($"获取 SKU {skuId} 库存锁超时");
            }

            if (!await stockRepository.HasFlowAsync(bizNo, skuId, "restore", cancellationToken))
            {
                await stockRepository.RestoreAsync(skuId, bizNo, quantity, cancellationToken);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null) await _channel.CloseAsync(cancellationToken);
        if (_connection is not null) await _connection.CloseAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
