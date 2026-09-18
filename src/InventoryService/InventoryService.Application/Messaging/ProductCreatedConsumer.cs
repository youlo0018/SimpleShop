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

/// <summary>
/// 商品创建消费者：按 SKU 幂等初始化库存，避免新商品因缺库存行而无法交易。
/// </summary>
public sealed class ProductCreatedConsumer(
    IConfiguration configuration,
    IStockRepository stockRepository,
    IDistributedLock distributedLock,
    ILogger<ProductCreatedConsumer> logger) : BackgroundService
{
    /// <summary>RabbitMQ 连接（懒加载，断线重建）。</summary>
    private IConnection? _connection;
    /// <summary>消费通道（随连接重建）。</summary>
    private IChannel? _channel;

    /// <summary>商品创建消费者：按 SKU 幂等初始化库存，避免新商品因缺库存行而无法交易。</summary>
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

    /// <summary>内部处理：StartCoreAsync。</summary>
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
        await _channel.QueueDeclareAsync("inventory.product.created", durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await _channel.QueueBindAsync("inventory.product.created", exchange, "product.created", cancellationToken: cancellationToken);

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
                logger.LogError(exception, "处理商品创建事件失败。");
                await _channel.BasicNackAsync(eventArgs.DeliveryTag, false, requeue: true);
            }
        };

        await _channel.BasicConsumeAsync("inventory.product.created", false, consumer, cancellationToken: cancellationToken);
    }

    /// <summary>内部处理：HandleMessage。</summary>
    private async Task HandleMessage(byte[] body, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(Encoding.UTF8.GetString(body));
        var root = document.RootElement.GetProperty("payload");
        var platformId = root.GetProperty("platformId").GetInt64();
        var merchantId = root.GetProperty("merchantId").GetInt64();

        foreach (var item in root.GetProperty("skuItems").EnumerateArray())
        {
            var skuId = item.GetProperty("skuId").GetInt64();
            var quantity = item.GetProperty("quantity").GetInt32();
            await using var lockHandle = await distributedLock.AcquireAsync(
                $"lock:stock-init:{skuId}", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(2), cancellationToken);
            if (lockHandle is null)
            {
                throw new TimeoutException($"获取 SKU {skuId} 初始化锁超时");
            }

            // 已存在库存时返回 false 是幂等成功，而不是消费失败。
            await stockRepository.InitializeAsync(skuId, platformId, merchantId, Math.Max(quantity, 0), cancellationToken);
        }
    }

    /// <summary>商品创建消费者：按 SKU 幂等初始化库存，避免新商品因缺库存行而无法交易。</summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null) await _channel.CloseAsync(cancellationToken);
        if (_connection is not null) await _connection.CloseAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
