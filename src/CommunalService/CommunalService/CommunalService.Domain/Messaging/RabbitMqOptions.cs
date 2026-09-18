namespace CommunalService.Domain.Messaging;

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
}
