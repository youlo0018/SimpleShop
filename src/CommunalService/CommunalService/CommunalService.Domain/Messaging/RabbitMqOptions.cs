namespace CommunalService.Domain.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMQ";

    public string HostName { get; set; } = "localhost";

    public string UserName { get; set; } = "guest";

    public string Password { get; set; } = "guest";

    public string Exchange { get; set; } = "simpleshop.events";
}
