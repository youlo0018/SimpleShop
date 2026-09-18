using System.Text;
using System.Text.Json;
using CommunalService.Domain.Infrastructure.Locks;
using InventoryService.Domain.IRepository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace InventoryService.Application.Messaging;

/// <summary>
/// 支付成功消费者：把下单时锁定的库存真正扣掉。
/// 事件里带 SKU 明细；同一 BizNo 重复投递时靠库存流水幂等跳过。
/// </summary>
public sealed class PaymentSucceededConsumer(
    IConfiguration configuration,
    IStockRepository stockRepository,
    IDistributedLock distributedLock,
    ILogger<PaymentSucceededConsumer> logger) : BackgroundService
{
    /// <summary>RabbitMQ 连接（懒加载，断线重建）。</summary>
    private IConnection? _connection;
    /// <summary>消费通道（随连接重建）。</summary>
    private IChannel? _channel;

    /// <summary>支付成功消费者：把下单时锁定的库存真正扣掉。 事件里带 SKU 明细；同一 BizNo 重复投递时靠库存流水幂等跳过。</summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // MQ 没起来时不要拖垮库存服务，后台循环重试即可。
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

    /// <summary>支付成功消费者：把下单时锁定的库存真正扣掉。 事件里带 SKU 明细；同一 BizNo 重复投递时靠库存流水幂等跳过。</summary>
    private async Task StartAsync(CancellationToken cancellationToken)
    {
        // 兼容 IConfiguration 与 IConfigurationRoot 的读取方式。
        var hostName = configuration.GetSection("RabbitMQ")["HostName"] ?? "localhost";
        var userName = configuration.GetSection("RabbitMQ")["UserName"] ?? "guest";
        var password = configuration.GetSection("RabbitMQ")["Password"] ?? "guest";
        var exchange = configuration.GetSection("RabbitMQ")["Exchange"] ?? "simpleshop.events";

        var factory = new ConnectionFactory { HostName = hostName, UserName = userName, Password = password };
        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await _channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);
        await _channel.QueueDeclareAsync("inventory.payment.succeeded", durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await _channel.QueueBindAsync("inventory.payment.succeeded", exchange, "payment.succeeded", cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            try
            {
                await HandleMessage(eventArgs.Body.ToArray(), eventArgs.CancellationToken);
                await _channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "处理支付成功事件失败。");
                await _channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false);
            }
        };

        await _channel.BasicConsumeAsync("inventory.payment.succeeded", autoAck: false, consumer, cancellationToken: cancellationToken);
    }

    /// <summary>内部处理：HandleMessage。</summary>
    private async Task HandleMessage(byte[] body, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(Encoding.UTF8.GetString(body));
        var root = document.RootElement.GetProperty("payload");
        var bizNo = root.GetProperty("bizNo").GetString() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(bizNo))
        {
            return;
        }

        foreach (var item in root.GetProperty("stockItems").EnumerateArray())
        {
            var skuId = item.GetProperty("skuId").GetInt64();
            var quantity = item.GetProperty("quantity").GetInt32();

            await using var lockHandle = await distributedLock.AcquireAsync(
                $"lock:stock:{skuId}",
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(2),
                cancellationToken);

            if (lockHandle is null)
            {
                throw new InvalidOperationException($"获取 SKU {skuId} 库存锁超时");
            }

            if (!await stockRepository.HasFlowAsync(bizNo, skuId, "deduct", cancellationToken))
            {
                await stockRepository.DeductAsync(skuId, bizNo, quantity, cancellationToken);
            }
        }
    }

    /// <summary>支付成功消费者：把下单时锁定的库存真正扣掉。 事件里带 SKU 明细；同一 BizNo 重复投递时靠库存流水幂等跳过。</summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _channel?.CloseAsync(cancellationToken);
        _connection?.CloseAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
