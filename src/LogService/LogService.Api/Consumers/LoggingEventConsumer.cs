using System.Diagnostics;
using Elastic.Clients.Elasticsearch;
using LogService.Api.Elasticsearch;
using Microsoft.Extensions.Options;
using Prometheus;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace LogService.Api.Consumers;

/// <summary>
/// 日志消费者：把 PV、业务操作、异常三类日志统一接入 Elasticsearch。
/// 本机没有 MQ 时服务照常启动，后台重连；处理失败的日志进入 DLQ，不丢也不无限循环。
/// </summary>
public sealed class LoggingEventConsumer(
    IOptions<RabbitMqOptions> rabbitOptions,
    ElasticsearchLogWriter logWriter,
    ILogger<LoggingEventConsumer> logger) : BackgroundService
{
    /// <summary>消费成功计数（Prometheus）。</summary>
    private static readonly Counter ConsumeSuccess = Prometheus.Metrics.CreateCounter(
        "logservice_messages_succeeded_total", "成功写入 Elasticsearch 的日志数量", new[] { "kind" });

    /// <summary>消费失败计数（Prometheus）。</summary>
    private static readonly Counter ConsumeFailed = Prometheus.Metrics.CreateCounter(
        "logservice_messages_failed_total", "进入死信队列的日志数量", new[] { "kind" });

    /// <summary>ES 写入耗时直方图（Prometheus）。</summary>
    private static readonly Histogram ElasticsearchWriteDuration = Prometheus.Metrics.CreateHistogram(
        "logservice_elasticsearch_write_seconds", "Elasticsearch 写入耗时", new[] { "kind" });

    /// <summary>RabbitMQ 配置快照。</summary>
    private readonly RabbitMqOptions _rabbit = rabbitOptions.Value;
    /// <summary>RabbitMQ 连接（懒加载，断线重连）。</summary>
    private IConnection? _connection;
    /// <summary>消费通道。</summary>
    private IChannel? _channel;

    /// <summary>日志消费者：把 PV、业务操作、异常三类日志统一接入 Elasticsearch。 本机没有 MQ 时服务照常启动，后台重连；处理失败的日志进入 DLQ，不丢也不无限循环。</summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConnectAndConsumeAsync(stoppingToken);
                return;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "连接 RabbitMQ 失败，10 秒后重试日志消费。");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    /// <summary>内部处理：ConnectAndConsumeAsync。</summary>
    private async Task ConnectAndConsumeAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _rabbit.HostName,
            UserName = _rabbit.UserName,
            Password = _rabbit.Password,
            AutomaticRecoveryEnabled = true
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await _channel.ExchangeDeclareAsync(_rabbit.Exchange, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: cancellationToken);
        await _channel.ExchangeDeclareAsync(_rabbit.DeadLetterExchange, ExchangeType.Direct, durable: true, autoDelete: false, cancellationToken: cancellationToken);

        // 主队列绑定死信交换机：Nack 后消息落到 DLQ，坏数据留给人工修复。
        await _channel.QueueDeclareAsync(
            _rabbit.Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = _rabbit.DeadLetterExchange,
                ["x-dead-letter-routing-key"] = _rabbit.Queue
            },
            cancellationToken: cancellationToken);
        await _channel.QueueDeclareAsync(_rabbit.DeadLetterQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await _channel.QueueBindAsync(_rabbit.Queue, _rabbit.Exchange, "pv.log", cancellationToken: cancellationToken);
        await _channel.QueueBindAsync(_rabbit.Queue, _rabbit.Exchange, "operation.log", cancellationToken: cancellationToken);
        await _channel.QueueBindAsync(_rabbit.Queue, _rabbit.Exchange, "exception.log", cancellationToken: cancellationToken);
        await _channel.QueueBindAsync(_rabbit.DeadLetterQueue, _rabbit.DeadLetterExchange, _rabbit.Queue, cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            var logKind = eventArgs.RoutingKey switch
            {
                "pv.log" => "pv",
                "operation.log" => "operation",
                _ => "exception"
            };
            var stopwatch = Stopwatch.StartNew();

            try
            {
                using var timer = ElasticsearchWriteDuration.WithLabels(logKind).NewTimer();
                await logWriter.WriteAsync(eventArgs.Body.ToArray(), logKind, eventArgs.CancellationToken);
                await _channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
                ConsumeSuccess.WithLabels(logKind).Inc();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "处理日志失败并转入 DLQ，RoutingKey：{RoutingKey}。", eventArgs.RoutingKey);
                await _channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false);
                ConsumeFailed.WithLabels(logKind).Inc();
            }
            finally
            {
                stopwatch.Stop();
            }
        };

        await _channel.BasicConsumeAsync(_rabbit.Queue, autoAck: false, consumer, cancellationToken);
    }

    /// <summary>日志消费者：把 PV、业务操作、异常三类日志统一接入 Elasticsearch。 本机没有 MQ 时服务照常启动，后台重连；处理失败的日志进入 DLQ，不丢也不无限循环。</summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken);
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync(cancellationToken);
        }

        await base.StopAsync(cancellationToken);
    }
}
