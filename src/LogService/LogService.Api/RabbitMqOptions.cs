namespace LogService.Api;

/// <summary>
/// 与其他微服务保持同一套 RabbitMQ 配置契约。
/// </summary>
public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMQ";

    /// <summary>RabbitMQ 主机。</summary>
    public string HostName { get; set; } = "localhost";

    /// <summary>登录名。</summary>
    public string UserName { get; set; } = "guest";

    /// <summary>密码（仅传输明文，落库为加盐散列）。</summary>
    public string Password { get; set; } = "guest";

    /// <summary>事件交换机名。</summary>
    public string Exchange { get; set; } = "simpleshop.events";

    /// <summary>主队列名，所有日志消费都从这里进入。</summary>
    public string Queue { get; set; } = "logservice.logs";

    /// <summary>死信交换机：坏日志不丢、不循环重试，留给人工排查。</summary>
    public string DeadLetterExchange { get; set; } = "logservice.dlx";

    /// <summary>死信队列名。</summary>
    public string DeadLetterQueue { get; set; } = "logservice.logs.dlq";
}
