namespace SharedKernel.Configuration;

public sealed class RabbitMqConfig
{
    public const string Name = "RabbitMQ";

    public string Host { get; set; } = "__RABBITMQ_HOST__";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "__RABBITMQ_USERNAME__";
    public string Password { get; set; } = "__RABBITMQ_PASSWORD__";
    public string VirtualHost { get; set; } = "__RABBITMQ_VIRTUAL_HOST__";
    public string Exchange { get; set; } = "__RABBITMQ_EXCHANGE__";
}
