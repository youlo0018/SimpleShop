namespace LogService.Api;

/// <summary>
/// 与其他微服务保持同一套 RabbitMQ 配置契约。
/// </summary>
public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMQ";

    public string HostName { get; set; } = "localhost";

    public string UserName { get; set; } = "guest";

    public string Password { get; set; } = "guest";

    public string Exchange { get; set; } = "simpleshop.events";

    /// <summary>主队列名，所有日志消费都从这里进入。</summary>
    public string Queue { get; set; } = "logservice.logs";

    /// <summary>死信交换机：坏日志不丢、不循环重试，留给人工排查。</summary>
    public string DeadLetterExchange { get; set; } = "logservice.dlx";

    /// <summary>死信队列名。</summary>
    public string DeadLetterQueue { get; set; } = "logservice.logs.dlq";
}
